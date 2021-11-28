namespace MyTwitter.Server

open System.Collections.Concurrent
open FSharp.Data


type Post =
    { PostId: uint64
      Author: string
      At: string list
      Tags: string list
      Retweet: uint64 option
      Content: string }


module Post =
    let toJson p =
        JsonValue.Record
            [|"post_id", JsonValue.Number (decimal p.PostId);
              "author", JsonValue.String p.Author
              "at", JsonValue.Array (List.toArray p.At |> Array.map JsonValue.String)
              "tags", JsonValue.Array (List.toArray p.Tags |> Array.map JsonValue.String)
              "retweet", 
                  p.Retweet 
                  |> Option.map (decimal >> JsonValue.Number) 
                  |> Option.defaultValue JsonValue.Null
              "content", JsonValue.String p.Content|]
        

type PostManager (userManager: UserManager) =

    let posts = ConcurrentDictionary<uint64, Post> ()
    let postsSearchByTag = ConcurrentDictionary<string, ConcurrentBag<Post>> ()
    let postsSearchByAt = ConcurrentDictionary<string, ConcurrentBag<Post>> ()
    let postsSearchByUser = ConcurrentDictionary<string, ConcurrentBag<Post>> ()

    let maxPostId = ref 0UL

    let queryBy (dicBag: ConcurrentDictionary<string, ConcurrentBag<Post>>) (key: string) = 
        let mutable dummy = null
        if dicBag.TryGetValue (key, &dummy)
        then Seq.cast dummy
        else Seq.empty
        : Post seq

    let rec dumpContent (c: char) (input: string) = 
        let a = input.IndexOf c
        if a < 0 
        then []
        else
            let b = input.IndexOf (' ', a)
            if b < 0 
            then [input.[a..].Trim()]
            else input.[a..b].Trim() :: dumpContent c input.[ b + 1 .. ]

    member _.SendPost (author: User) (content: string) (retweet: uint64 option) =
        let ats = dumpContent '@' content
        let tags = dumpContent '#' content
        let post =
            lock maxPostId (fun () ->   
                let post =
                    { PostId = maxPostId.Value
                      Author = author.Username
                      At = ats
                      Tags = tags 
                      Retweet = retweet
                      Content = content }

                assert (posts.TryAdd (maxPostId.Value, post))
                    
                maxPostId.Value <- maxPostId.Value + 1UL
                post)

        let postJson = JsonValue.Array [|Post.toJson post|]

        tags
        |> List.iter (fun tag ->
            let bag =
                postsSearchByTag.GetOrAdd (
                    tag, 
                    (fun _ -> ConcurrentBag<Post>()))
            bag.Add post)

        ats
        |> List.iter (fun at ->
            let bag =
                postsSearchByAt.GetOrAdd (
                    at, 
                    (fun _ -> ConcurrentBag<Post>()))
            bag.Add post
            
            userManager.SendJsonToUser (at.[1..].Trim()) postJson)

        begin
            let bag =
                postsSearchByUser.GetOrAdd (
                    author.Username, 
                    (fun _ -> ConcurrentBag<Post>()))
            in bag.Add post
        end

        for follower in author.Follower do
            userManager.SendJsonToUser follower.Username postJson

    member _.QueryByTag = queryBy postsSearchByTag
    member _.QueryByAt = queryBy postsSearchByAt
    member _.QueryByUser = queryBy postsSearchByUser
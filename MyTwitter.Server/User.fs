namespace MyTwitter.Server

open System.Collections.Concurrent
open FSharp.Data


type User = 
    { Username: string
      Follower: ConcurrentBag<User>
      Following: ConcurrentBag<User> }


type UserManager () =
    
    let users = ConcurrentDictionary<string, User> ()
    
    member _.Register (username: string) = 
        if users.TryAdd (username, 
            { Username = username
              Follower = ConcurrentBag<User> ()
              Following = ConcurrentBag<User> () })
        then Ok ()
        else Error ()

    member _.FindUser (username: string) =
        let mutable dummy = { Username = ""; Follower = null; Following = null }
        if users.TryGetValue (username, &dummy)
        then Ok dummy
        else Error ()

    member x.Follow (follower: string) (followee: string) =
        x.FindUser follower
        |> Result.bind (fun follower ->
            x.FindUser followee
            |> Result.bind (fun followee ->
                followee.Follower.Add follower
                follower.Following.Add followee
                Ok ()))

    member _.SendJsonToUser (username: string) (json: JsonValue) =
        failwith "No Impl"
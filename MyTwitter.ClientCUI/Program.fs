open MyTwitter
open System


[<EntryPoint>]
let main _ =
    printfn "Welcome to MyTwitter!"
    printfn ""
    printfn "Send a Tweet: "
    printfn "    tweet Hello!"
    printfn "Retweet: "
    printfn "    retweet <post_id> Hello!"
    printfn "Follow someone: "
    printfn "    follow user1"
    printfn "Query @me:"
    printfn "    atme"
    printfn "Query:"
    printfn "    query #tag"
    printfn "    query @user"
    printfn "    query following"
    printfn "Quit: "
    printfn "    quit"
    printfn ""
    printf "Who are you: "
    let username = Console.ReadLine ()
    use client = new Client (username)
    Console.Title <- $"MyTwitter: {username}"
    Console.Clear ()

    let mutable loop = true

    let recv () =
        for i in client.Recv() do
            printfn "%A" i

    while loop do
        printf "> "
        let input = Console.ReadLine().Trim()
        let s = input.IndexOf ' '
        let command = if s = -1 then input else input.[..s].Trim()
        let content = if s = -1 then "" else input.[s..].Trim()

        match command with
        | "tweet" -> client.Tweet None content
        | "retweet" -> 
            let s = content.IndexOf ' '
            if s = -1 
            then printf "You need to input post id to retweet!"
            else
                let retweetPostId = uint64 <| content.[..s].Trim()
                let content = content.[s..].Trim()
                client.Tweet (Some retweetPostId) content
        | "follow" -> client.Follow content
        | "atme" -> client.QueryPostsAtMe (); recv ()
        | "query" ->
            match content with
            | "following" -> client.QueryPostsFollowing (); recv ()
            | x when x.StartsWith '#' -> client.QueryPostsByTag x; recv ()
            | x when x.StartsWith '@' -> client.QueryPostsByUser x.[1..]; recv ()
            | _ -> printfn "Invalid query!"
        | "recv" -> recv ()
        | "quit" -> loop <- false
        | _ -> printfn "Invalid command!"

    0


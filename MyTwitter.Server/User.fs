namespace MyTwitter.Server

open System.Collections.Concurrent
open FSharp.Data
open WebSharper.AspNetCore.WebSocket.Server

type C2S = 
    { op: string
      arg1: string
      arg2: string }


type User = 
    { Username: string
      Follower: ConcurrentBag<User>
      Following: ConcurrentBag<User>
      Connect: WebSocketClient<string, C2S> option }


type UserManager () =
    
    let users = ConcurrentDictionary<string, User> ()
    
    member _.Register (username: string) connect = 
        if 
            begin
                let user =
                    { Username = username
                      Follower = ConcurrentBag<User> ()
                      Following = ConcurrentBag<User> ()
                      Connect = Some connect }
                in users.TryAdd (username, user)
            end
        then Ok ()
        else Result.Error ()

    member _.Logout (username: string) =
        users.TryRemove (username) |> ignore

    member _.FindUser (username: string) =
        let mutable dummy = { Username = ""; Follower = null; Following = null; Connect = None }
        if users.TryGetValue (username, &dummy)
        then Ok dummy
        else Result.Error ()

    member x.Follow (follower: string) (followee: string) =
        x.FindUser follower
        |> Result.bind (fun follower ->
            x.FindUser followee
            |> Result.bind (fun followee ->
                followee.Follower.Add follower
                follower.Following.Add followee
                Ok ()))

    member _.SendJsonToUser (username: string) (json: JsonValue) =
        let mutable dummy = { Username = ""; Follower = null; Following = null; Connect = None }
        if users.TryGetValue (username, &dummy) then
            if dummy.Connect.IsSome then
                dummy.Connect.Value.Post <| json.ToString ()
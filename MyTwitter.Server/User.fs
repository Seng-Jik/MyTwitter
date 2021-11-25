namespace MyTwitter.Server

open System.Collections.Concurrent
open FSharp.Data
open Suave.Sockets
open Suave.WebSocket


type User = 
    { Username: string
      Follower: ConcurrentBag<User>
      Following: ConcurrentBag<User>
      Connect: WebSocket option }


type UserManager () =
    
    let users = ConcurrentDictionary<string, User> ()
    
    member _.Register (username: string) connect = 
        if users.TryAdd (username, 
            { Username = username
              Follower = ConcurrentBag<User> ()
              Following = ConcurrentBag<User> ()
              Connect = Some connect })
        then Ok ()
        else Error ()

    member _.Logout (username: string) =
        users.TryRemove (username) |> ignore

    member _.FindUser (username: string) =
        let mutable dummy = { Username = ""; Follower = null; Following = null; Connect = None }
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
        let mutable dummy = { Username = ""; Follower = null; Following = null; Connect = None }
        if users.TryGetValue (username, &dummy) then
            if dummy.Connect.IsSome then
                dummy.Connect.Value.send Text (json.ToString () |> UTF8.bytes |> ByteSegment) true
                |> Async.Ignore
                |> Async.Start
open MyTwitter.Server
open Suave
open Suave.Sockets.Control
open Suave.Sockets
open Suave.WebSocket
open FSharp.Data


type RegisterMessage = JsonProvider<"{ \"username\": \"me\" }">


let userMgr = UserManager ()
let postMgr = PostManager (userMgr)


type QueryRequest = JsonProvider<"{ \"op\": \"query_at\", \"arg1\": \"mark\", \"arg2\": \"what\" }">


let twitterService (webSocket: WebSocket) (_: HttpContext) =
    socket {
        let! _, username, _ = 
            webSocket.read ()

        let username =
            username
            |> UTF8.toString
            |> RegisterMessage.Parse
            |> fun x -> x.Username

        let mutable loop = true

        userMgr.Register username webSocket
        |> function
            | Ok _ -> webSocket.send Opcode.Text (UTF8.bytes "OK" |> ByteSegment) true
            | Error _ -> 
                loop <- false
                webSocket.send Close (UTF8.bytes "Failed" |> ByteSegment) true
        |> Async.RunSynchronously
        |> ignore

        let myself = 
            userMgr.FindUser username
            |> function
                | Ok u -> u
                | Error _ -> failwith "Can not get myself."

        lock stdout (fun () -> printfn $"{username} is login.")

        while loop do
            match! webSocket.read () with
            | (Opcode.Text, data, true) ->
                let json = UTF8.toString data |> QueryRequest.Parse

                let sendQueryResult =
                    Seq.map Post.toJson
                    >> Array.ofSeq
                    >> JsonValue.Array
                    >> fun x -> x.ToString ()
                    >> UTF8.bytes
                    >> ByteSegment
                    >> fun x -> webSocket.send Opcode.Text x true
                    >> Async.Ignore
                    >> Async.RunSynchronously

                match json.Op with
                | "query_posts_by_at" -> postMgr.QueryByAt json.Arg1 |> sendQueryResult
                | "query_posts_by_user" -> postMgr.QueryByUser json.Arg1 |> sendQueryResult
                | "query_posts_by_tag" -> postMgr.QueryByTag json.Arg1 |> sendQueryResult
                | "follow" -> userMgr.Follow username json.Arg1 |> ignore
                | "tweet" -> 
                    postMgr.SendPost myself json.Arg1 <|
                        if json.Arg2 = "" 
                        then None 
                        else Some <| uint64 (json.Arg2.AsInteger64 ())
                    
                | _ -> ()
            | (Close, _, _) -> 
                loop <- false
                userMgr.Logout username
            | _ -> ()

        lock stdout (fun () -> printfn $"{username} is logout.")
    }


let config =
    { defaultConfig with 
        bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" 31755 ] }


let _, server = startWebServerAsync config <| handShake twitterService
    

Async.RunSynchronously server


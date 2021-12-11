module MyTwitter.Server.Server
open MyTwitter.Server
open MyTwitter.Server.Post
open WebSharper.AspNetCore.WebSocket.Server
open FSharp.Data


type RegisterMessage = JsonProvider<"{ \"username\": \"me\" }">


let userMgr = UserManager ()
let postMgr = PostManager (userMgr)


type QueryRequest = JsonProvider<"{ \"op\": \"query_at\", \"arg1\": \"mark\", \"arg2\": \"what\" }">


let start () : StatefulAgent<string, C2S, User option> =
    fun client -> async {

        return None, fun myself msg -> async {
            match myself, msg with
            | None, Message msg -> 
                let username = msg.arg1
                match userMgr.Register username client with
                | Ok _ -> 
                    client.Post "OK"
                    let myself = 
                        userMgr.FindUser username
                        |> function
                            | Ok u -> u
                            | Result.Error _ -> failwith "Can not get myself."
                    return Some myself

                | Result.Error _ -> 
                    client
                        .Connection
                        .Close(
                            System.Net.WebSockets.WebSocketCloseStatus.Empty, 
                            "Failed")
                        .RunSynchronously()

                    lock stdout (fun () -> printfn $"{username} is login.")
                    return None
            | (None, _) -> return myself
            | (Some myself, Message msg) ->
                let json = msg

                let sendQueryResult =
                    Seq.map Post.toJson
                    >> Array.ofSeq
                    >> JsonValue.Array
                    >> fun x -> x.ToString ()
                    >> client.Post

                match json.op with
                | "query_posts_by_at" -> postMgr.QueryByAt json.arg1 |> sendQueryResult
                | "query_posts_by_user" -> postMgr.QueryByUser json.arg1 |> sendQueryResult
                | "query_posts_by_tag" -> postMgr.QueryByTag json.arg1 |> sendQueryResult
                | "follow" -> 
                    userMgr.Follow myself.Username json.arg1 |> ignore
                    
                | "tweet" -> 
                    postMgr.SendPost myself json.arg1 <|
                        if json.arg2 = "" 
                        then None 
                        else Some <| uint64 (json.arg2.AsInteger64 ())
                    
                | _ -> ()

                return Some myself

            | (Some myself, Close) ->
                userMgr.Logout myself.Username
                lock stdout (fun () -> printfn $"{myself.Username} is logout.")
                return None
            | _ -> return myself
        }
    }
    



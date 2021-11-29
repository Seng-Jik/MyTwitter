namespace MyTwitter

open System
open System.Net.WebSockets
open FSharp.Data


type Post =
    { PostId: uint64
      Author: string
      At: string list
      Tags: string list 
      Retweet: uint64 option 
      Content: string }


module private SrvMsgHelper =

    [<Literal>]
    let srvJsonSmp =
        """[{ 
                "post_id": 12345, 
                "author": "somebody", 
                "at": [ "one", "two", "three" ],
                "tags": [ "tag1", "tag2", "tag3" ],
                "retweet": null,
                "content": "abc!!"
            },
            {
                "post_id": 123456,
                "author": "somebody",
                "at": [],
                "tags": [],
                "retweet": 1234567,
                "content": "wtf"
            }]"""


    type PostsJson = JsonProvider<srvJsonSmp>


    let toPost str =
        let json = PostsJson.Parse str
        json
        |> Seq.map (fun x ->
            { PostId = uint64 x.PostId
              At = Array.toList x.At
              Tags = Array.toList x.Tags
              Retweet = Option.map uint64 x.Retweet
              Author = x.Author
              Content = x.Content })
        |> Seq.toList


type Client (username: string) =
    let wsClient = new ClientWebSocket ()

    let recvBuffer = new Memory<byte> (Array.init 65536 (fun _ -> 0uy))
    let recvBuffer'Lock = obj ()

    let sendJson (json: JsonValue) = 
        let bytes = json.ToString() |> Text.Encoding.UTF8.GetBytes |> Memory
        let cancellationToken = Threading.CancellationToken ()
        wsClient.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).GetAwaiter().GetResult()

    let recvText () = 
        lock recvBuffer'Lock (fun () -> 
            let cancellationToken = Threading.CancellationToken ()
            let c = wsClient.ReceiveAsync(recvBuffer, cancellationToken).AsTask().Result.Count
            let data = recvBuffer.Slice(0, c).Span
            Text.Encoding.UTF8.GetString data)

    let mutable isOk = false

    do 
        let cancellationToken = Threading.CancellationToken ()
        wsClient.ConnectAsync(Uri "ws://localhost:31755/", cancellationToken).Wait()
        let registerMsg = JsonValue.Record [|("username", JsonValue.String username)|]
        sendJson registerMsg
        match recvText () with
        | "OK" -> isOk <- true
        | _ -> isOk <- false

    let makeMsg opCode arg1 arg2 =
        JsonValue.Record
          [|"op", JsonValue.String opCode
            "arg1", JsonValue.String arg1
            "arg2", JsonValue.String arg2|]

    let sendMsg opCode arg1 arg2 = makeMsg opCode arg1 arg2 |> sendJson

    member _.Username = username
    
    member _.Follow username =
        sendMsg "follow" username ""

    member _.Tweet (retweet: uint64 option) (content: string) =
        sendMsg "tweet" content (retweet |> Option.map string |> Option.defaultValue "")

    member _.QueryPostsAtMe () =
        sendMsg "query_posts_by_at" "@{username}" ""

    member _.QueryPostsByTag tag =
        sendMsg "query_posts_by_tag" tag ""

    member _.QueryPostsByUser user =
        sendMsg "query_posts_by_user" user ""

    member _.Recv () = recvText () |> SrvMsgHelper.toPost

    interface IDisposable with
        member _.Dispose () = 
            if isOk then
                let cancellationToken = Threading.CancellationToken ()
                wsClient.CloseOutputAsync(WebSocketCloseStatus.Empty, "", cancellationToken).GetAwaiter().GetResult()
                wsClient.Dispose ()
                isOk <- false
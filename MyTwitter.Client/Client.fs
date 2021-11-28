namespace MyTwitter

open System
open System.Net.WebSockets
open FSharp.Data


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

    let recvJson = recvText >> JsonValue.Parse

    let mutable isOk = false

    do 
        let cancellationToken = Threading.CancellationToken ()
        wsClient.ConnectAsync(Uri "ws://localhost:31755/", cancellationToken).Wait()
        let registerMsg = JsonValue.Record [|("username", JsonValue.String username)|]
        sendJson registerMsg
        match recvText () with
        | "OK" -> isOk <- true
        | _ -> isOk <- false

    interface IDisposable with
        member _.Dispose () = 
            if isOk then
                let cancellationToken = Threading.CancellationToken ()
                wsClient.CloseOutputAsync(WebSocketCloseStatus.Empty, "", cancellationToken).GetAwaiter().GetResult()
                wsClient.Dispose ()
                isOk <- false
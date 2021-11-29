open MyTwitter
open System


let pid = Environment.ProcessId


let tweets = 10000


let stars = 
    Array.Parallel.init 50 (fun x -> new Client ($"star{x}_in_pid_{pid}"))


let users =
    Array.Parallel.init 1000 (fun x -> new Client ($"user{x}_in_pid_{pid}"))


for (i, star) in stars |> Seq.indexed do
    let followers = 
        let count = Array.length users / (i + 1)
        users |> Seq.take count

    for follower in followers do
        follower.Follow star.Username


let userThreads =
    users 
    |> Array.map (fun client -> async { while true do ignore (client.Recv()) })
    |> Async.Parallel
    |> Async.Ignore
    |> Async.StartAsTask


let timer = Diagnostics.Stopwatch ()
timer.Start ()

printfn $"{Array.length users} users listen to {Array.length stars} stars (in Zipf's law), every star send {tweets} tweets..."

stars 
|> Array.map (fun client -> async { 
    for _ in 1..tweets do
        client.Tweet None "Tweet!!!"
})
|> Async.Parallel
|> Async.Ignore
|> Async.RunSynchronously


timer.Stop ()

printfn "%A" timer.Elapsed

for i in users do 
    (i :> IDisposable).Dispose()

for i in stars do 
    (i :> IDisposable).Dispose()
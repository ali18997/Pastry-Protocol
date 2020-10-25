open Akka
open Akka.FSharp
open System
open System.Diagnostics
open Akka.Actor
open System


//Create System reference
let system = System.create "system" <| Configuration.defaultConfig()

let mutable numNodes = 0
let mutable numRequests = 0
let mutable arrayActor : IActorRef array = null
let mutable exitNodes = 0

type Message() = 
    [<DefaultValue>] val mutable num: int
    [<DefaultValue>] val mutable from: int

let killActor num = 
    arrayActor.[int num] <! PoisonPill.Instance

let killAll start = 
    for i = 0 to numNodes-1 do
        killActor i

let getNeighbour currentNum = 
    let objrandom = new Random()
    let ran = objrandom.Next(0,numNodes)
    ran

let sendMessage num from = 
    let sendMsg = new Message()
    sendMsg.num <- num
    sendMsg.from <- from
    arrayActor.[int num] <! sendMsg



//Actor
let actor (actorMailbox:Actor<Message>) = 
    //Actor Loop that will process a message on each iteration
    let mutable count = 0
    let mutable flag = true
    let timer = new Stopwatch()
    let rec actorLoop() = actor {

        //Receive the message
        let! msg = actorMailbox.Receive()
        
        if count = 0 then
            timer.Start()
            count <- count + 1
            //printfn "Actor %A Count %A" msg.num count
        let currentTime = double (timer.ElapsedMilliseconds)
        
        if msg.from <> msg.num then
            count <- count
        elif currentTime > 1000.0 && count < numRequests + 2 then
            timer.Reset()
            timer.Start()
            count <- count + 1
            //printfn "Actor %A Count %A" msg.num count
            sendMessage (getNeighbour msg.num) (msg.num)
        elif count >= numRequests + 2 then
            timer.Stop()
            if flag then
                exitNodes <- exitNodes + 1
                flag <- false
                //printfn "Actor %A exited Exit Count %A" msg.num exitNodes
                if exitNodes = numNodes then
                    printfn "Done"
                    killAll true

        if flag then
            sendMessage (msg.num) (msg.num)
        
        return! actorLoop()
    }

    //Call to start the actor loop
    actorLoop()

let makeActors start =
    arrayActor <- Array.zeroCreate numNodes

    for i = 0 to numNodes-1 do
        let name:string = "actor" + i.ToString() 
        arrayActor.[i] <- spawn system name actor 



[<EntryPoint>]
let main (args) =
    numNodes <- args.[0] |> int
    numRequests <- args.[1] |> int
    
    makeActors true

    sendMessage 0 0

    System.Console.ReadKey() |> ignore

    0 // return an integer exit code

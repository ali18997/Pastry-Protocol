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

let addZeros num zeroNum = 
    let mutable zeros = ""
    for i = 0 to zeroNum-1 do
        zeros <- zeros + "0"
    zeros + num

let rec intToBinary2 i =
    match i with
    | 0 | 1 -> string i
    | _ ->
        let bit = string (i % 2)
        (intToBinary2 (i / 2)) + bit

let intToBinary i = 
    let binNum = intToBinary2 i
    (addZeros binNum (32-binNum.Length))

let mismatch num dest = 
    let num = (string) num
    let dest = (string) dest
    let mutable next = ""
    let mutable retVal = -1
    for i = 0 to 31 do
        if dest.[i] <> num.[i] then
            if retVal = -1 then
                retVal <- i
                next <- next + (string)dest.[i]
            else
                next <- next + (string)num.[i]
        else
            next <- next + (string)num.[i]
    retVal, next
    
let route num dest =
    let binNum = intToBinary num
    let binDest = intToBinary dest

    let mismatch1, mismatch2 = (mismatch binNum binDest)
    
    printfn "%A %A" binNum binDest
    printfn "%A %A" mismatch1 mismatch2

    let mismatch3, mismatch4 = (mismatch mismatch2 binDest)

    printfn "%A %A" mismatch3 mismatch4

    let mismatch5, mismatch6 = (mismatch mismatch4 binDest)

    printfn "%A %A" mismatch5 mismatch6

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

    route (10 ) (40) 

    System.Console.ReadKey() |> ignore

    0 // return an integer exit code

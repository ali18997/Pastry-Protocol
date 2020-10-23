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

[<EntryPoint>]
let main (args) =
    numNodes <- args.[0] |> int
    numRequests <- args.[1] |> int


    0 // return an integer exit code

module Crawler.Program

open Crawler
open Crawler.Common
open System
open System.Collections.Generic
open System.IO

let rec parseCommandLine (args: string list) (kwargMap: Map<string, string>) (argList: string list) =
    match args with
    | [] ->
        (kwargMap, argList)
        
    |  key::value::tail when key.StartsWith "--" ->
        let newMap = kwargMap.Add(key, value)
        parseCommandLine tail newMap argList
    
    | arg::tail ->
        let newList = arg :: argList
        parseCommandLine tail kwargMap newList
        
[<EntryPoint>]
let main args =
    let (kwargMap, argListRev) = parseCommandLine (args |> Array.toList)  Map.empty []
    let argList = argListRev |> List.rev
    
    // TODO Raise exception when argList is empty
    let seedUrls = File.ReadLines argList.Head |> Seq.toList
    
    let maxDepthOption = kwargMap.TryFind "--max-depth"
    let maxDepth =
        match maxDepthOption with 
            | Some str -> str |> int
            | None -> 3
    let outputDir = kwargMap.TryFind "--output"
    
    let master = Master.Agent seedUrls maxDepth outputDir
    master.Post MasterMessage.Start
    
    Console.ReadLine() |> ignore
    0
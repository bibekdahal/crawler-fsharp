module crawler.Program

open crawler
open crawler.Common
open System
open System.IO

exception InvalidCommandLine

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
    try
        let (kwargMap, argListRev) = parseCommandLine (args |> Array.toList)  Map.empty []
        let argList = argListRev |> List.rev
        if argList.Length = 0 then raise InvalidCommandLine
    
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
    with
        InvalidCommandLine ->
            Console.WriteLine("Usage: crawler --max-depth <number> --output <output_folder> <input_file>")
    0
module crawler.Program

open crawler
open crawler.Common
open System
open System.IO

exception InvalidCommandLine

// A recursive function that takes a list of command line arguments and converts them into
// a map of keyword arguments and a list of positional arguments.
let rec parseCommandLine (args: string list) (kwargMap: Map<string, string>) (argList: string list) =
    match args with
    | [] ->
        (kwargMap, argList |> List.rev)
        
    |  key::value::tail when key.StartsWith "--" ->
        let newMap = kwargMap.Add(key, value)
        parseCommandLine tail newMap argList
    
    | arg::tail ->
        let newList = arg :: argList
        parseCommandLine tail kwargMap newList
        
[<EntryPoint>]
let main args =
    try
        // Parse the command line arguments
        let (kwargMap, argList) = parseCommandLine (args |> Array.toList) Map.empty []
        if argList.Length = 0 then raise InvalidCommandLine
    
        // Read in the options: seed URLs, max depth, output folder and the scale factor for workers
        let seedUrls = File.ReadLines argList.Head |> Seq.toList
        let maxDepth =
            match (kwargMap.TryFind "--max-depth") with 
                | Some str -> str |> int
                | None -> 3
        let outputDir = kwargMap.TryFind "--output"
        let scaleFactor =
            match (kwargMap.TryFind "--scale") with
               | Some str -> str |> float
               | None -> 2.0
        let downloadFunction = Downloader.downloadUrl outputDir
   
        // Create the master actor which will coordinate all the actors
        let master = Master.Agent seedUrls maxDepth downloadFunction scaleFactor

        // Start the web crawling process
        master.Post MasterMessage.Start
    
        Console.ReadLine() |> ignore
    with
        InvalidCommandLine ->
            Console.WriteLine("Usage: crawler --max-depth <number> --output <output_folder> <input_file>")
    0
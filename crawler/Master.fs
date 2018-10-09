module crawler.Master

open System
open System.Collections.Generic
open Common

let Agent (seedUrls: string list) (maxDepth: int) (outputDir: Option<string>) = MailboxProcessor<MasterMessage>.Start(fun inbox ->
    let workers = [ 1..Environment.ProcessorCount ] |> List.map (fun _ -> Worker.Agent inbox outputDir)
    
    let completedUrls = new HashSet<string>()
    let pendingPages = new Queue<Page>()

    let rec loop() = async {
        let! msg = inbox.Receive()
        
        match msg with 
        | Start ->
            seedUrls |> List.iter (fun url -> pendingPages.Enqueue { url = url; nestLevel = 0 })
            workers |> List.iter (fun worker -> worker.Post WorkerMessage.WorkAvailable)
            
        | MasterMessage.RequestPage worker ->
            if pendingPages.Count > 0 then
                let page = pendingPages.Dequeue()
                completedUrls.Add page.url |> ignore
                worker.Post (WorkerMessage.ProcessPage page)
                
        | MasterMessage.OnNewPages pages ->
            pages
                |> List.filter(fun p ->
                    p.nestLevel <= maxDepth &&
                    not (completedUrls.Contains p.url))
                |> List.iter(fun p -> pendingPages.Enqueue p)
                
            workers |> List.iter(fun worker -> worker.Post WorkerMessage.WorkAvailable)
        
        return! loop()
    }
    loop())
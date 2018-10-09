module crawler.Master

open System
open System.Collections.Generic
open Common


(*
Master is an actor which is responsible for creating and coordinating
all the worker crawlers. It also maintains the list of completed URLs and
a queue of pending URLs.

Following arguments are taken by Agent:
    - list of seed URLs
    - maximum depth of crawling
    - downloader that downloads files from URLs
    - scale factor to scale the number of workers
*)
let Agent (seedUrls: string list) (maxDepth: int) (downloadFunction: string -> unit) (scaleFactor: float) =
    MailboxProcessor<MasterMessage>.Start(fun inbox ->
        // Calculate the number of workers in proportion ot the number of logical processors in the machine.
        // Multiply the numProcessors by scaleFactor to get the actual number of workers.
        let numProcessors = Environment.ProcessorCount |> float
        let numWorkers = (numProcessors * scaleFactor) |> int

        // The actual workers.
        let workers = [ 1..numWorkers ] |> List.map (fun _ -> Worker.Agent inbox downloadFunction)
    
        // Master needs to keep track of a list of completed URLs and a queue onf pending URLs.
        // For a pending URL, it is helpful to know the depth of the crawling we have reached
        // when the URL was first retrieved. So we store this info in the form: Page{url;depth}.
        let completedUrls = new HashSet<string>()
        let pendingPages = new Queue<Page>()

        // Helper function to check if a url is already in either the completed list or in the pending queue.
        let containsUrl (url: string) =
            (completedUrls.Contains url) ||
            (pendingPages |> Seq.exists(fun p -> p.url = url))

        // Start fetching the messages and processing them.
        let rec loop() = async {
            let! msg = inbox.Receive()
        
            match msg with 
            // On start,
            // * fill the seed URLs into the pendingPages with depth set to zero
            // * tell each worker that a work has been available
            | Start ->
                seedUrls |> List.iter (fun url -> pendingPages.Enqueue { url = url; nestLevel = 0 })
                workers |> List.iter (fun worker -> worker.Post WorkerMessage.WorkAvailable)
            
            // When a worker requests for a new page from the pending queue,
            // check if the queue is not empty, and if not give that page to the worker.
            // Note that wee also need to mark this URL as done by storing it in the completed list.
            | MasterMessage.RequestPage worker ->
                if pendingPages.Count > 0 then
                    let page = pendingPages.Dequeue()
                    completedUrls.Add page.url |> ignore
                    worker.Post (WorkerMessage.ProcessPage page)

            // When a worker gives the master a list of new pages,
            // store them in the pending queue and notify all workers that URLs are now available for processing.
            // We need to however first filter the incoming URLs by checking the following conditions:
            // * Depth is not more than the max-depth value
            // * The URL is not already in the completed list or the pending queue
            | MasterMessage.OnNewPages pages ->
                pages
                    |> List.filter(fun p ->
                        p.nestLevel <= maxDepth &&
                        not (containsUrl p.url))
                    |> List.iter(fun p -> pendingPages.Enqueue p)
                workers |> List.iter(fun worker -> worker.Post WorkerMessage.WorkAvailable)
        
            return! loop()
        }
        loop())
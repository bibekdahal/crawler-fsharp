module Cralwer.Worker
open System
open System.Threading
open Common

    
let mutable index = 0
let Agent (master: MailboxProcessor<MasterMessage>) = MailboxProcessor.Start(fun inbox ->
    let uniqueWorker = index + 1
    index <- index + 1
    
    let rec loop() = async {
        let! msg = inbox.Receive()
        match msg with 
        | WorkAvailable ->
            if inbox.CurrentQueueLength = 0 then
                master.Post (MasterMessage.RequestPage inbox)
                
        | ProcessPage page ->
            Console.WriteLine (page.url + " " + (uniqueWorker |> string))
            Thread.Sleep 2000
            master.Post(MasterMessage.OnNewPages([]))
        return! loop()
    }
    loop()
)
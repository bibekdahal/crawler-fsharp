module crawler.Worker

open Common

    
let mutable index = 0
let Agent (master: MailboxProcessor<MasterMessage>) (outputDir: Option<string>) = MailboxProcessor.Start(fun inbox ->
    let uniqueWorker = index + 1
    index <- index + 1
    
    let rec loop() = async {
        let! msg = inbox.Receive()
        match msg with 
        | WorkAvailable ->
            master.Post (MasterMessage.RequestPage inbox)
                
        | ProcessPage page ->
            if UrlValidator.validateUrl page.url then
                Downloader.downloadUrl page.url outputDir 
                let urls = UrlsExtractor.extract page.url
                let pages = urls |> List.map (fun url -> { url = url; nestLevel = page.nestLevel + 1})
                master.Post(MasterMessage.OnNewPages pages)
        return! loop()
    }
    loop()
)
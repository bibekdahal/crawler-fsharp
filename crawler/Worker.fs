module crawler.Worker

open Common

    
(*
Worker is the Actor handling the job of an independent crawler.
It takes a URL from the master and process that URL to retrieve more URLS by scraping.
*)

let Agent (master: MailboxProcessor<MasterMessage>) (downloadFunction: string -> unit) =
    MailboxProcessor.Start(fun inbox ->
        // The message handlers:
        let rec loop() = async {
            let! msg = inbox.Receive()
            match msg with 

            // Master notified us that a URL was available.
            // So send a request if it's still available.
            | WorkAvailable ->
                master.Post (MasterMessage.RequestPage inbox)
      
            // On behalf of our request, master has sent us a URL for crawling.
            | ProcessPage page ->
                // First, validate the URL by verifying we can access it and checking the robots.txt rules
                if UrlValidator.validateUrl page.url then
                    // Download file from the validated URL
                    downloadFunction page.url
                    // Extract new list of URLs by scraping
                    let urls = UrlsExtractor.extract page.url
                    // The new URLs now have depth, current + 1
                    let pages = urls |> List.map (fun url -> { url = url; nestLevel = page.nestLevel + 1})

                    // Send the new URLs to the master
                    master.Post(MasterMessage.OnNewPages pages)
            return! loop()
        }
        loop()
    )
module crawler.Common

type Page = {url: string; nestLevel: int}

type WorkerMessage =
    | WorkAvailable
    | ProcessPage of Page
    
type MasterMessage =
    | Start
    | RequestPage of MailboxProcessor<WorkerMessage> 
    | OnNewPages of Page list

let USER_AGENT = "Mozilla/5.0 (compatible; Crawler/1.0; +http://crawler.com)"

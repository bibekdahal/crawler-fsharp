module Cralwer.Common

type Page = {url: string; nestLevel: int}

type WorkerMessage =
    | WorkAvailable
    | ProcessPage of Page
    
type MasterMessage =
    | Start
    | RequestPage of MailboxProcessor<WorkerMessage> 
    | OnNewPages of Page list

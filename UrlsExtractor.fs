module Cralwer.UrlsExtractor

open System
open System.Net
open HtmlAgilityPack

let fetch (url: string) = async {
    let req = WebRequest.Create(Uri(url))
    let stream = req.GetResponse().GetResponseStream()
    let reader = new IO.StreamReader(stream)
    let html = reader.ReadToEnd()
    
    let doc = new HtmlDocument()
    doc.LoadHtml html
    let links = doc.DocumentNode.Descendants("a")
    
    let linkList =
        List.ofSeq(links)
        |> List.map (fun node -> node.GetAttributeValue("href", null))
        |> List.where (fun item -> not(String.IsNullOrEmpty(item)))
    
    printfn "%A" linkList
}

//let urls = ["http://titan.dcs.bbk.ac.uk/~kikpef01/testpage.html"]
//
//urls
//|> List.map fetch
//|> Async.Parallel
//|> Async.RunSynchronously
//|> ignore
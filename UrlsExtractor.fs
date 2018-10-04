module Cralwer.UrlsExtractor

open System
open System.Net
open HtmlAgilityPack

// TODO: User Agent


let transformPath (url: string) (path: string) =
    let (canCreate, uri) = Uri.TryCreate(path, UriKind.Absolute)
    if canCreate then
        path
    else if path.StartsWith "/" then
        let sourceUri = Uri(url)
        let uri = (new UriBuilder(sourceUri.Scheme, sourceUri.Host, sourceUri.Port, path)).Uri
        uri.ToString()
    else if url.EndsWith "/" then 
        url + path
    else 
        url + "/" + path

let extract (url: string) =            
    try
        let req = WebRequest.Create(Uri(url))
        let stream = req.GetResponse().GetResponseStream()
        let reader = new IO.StreamReader(stream)
        let html = reader.ReadToEnd()
        
        let doc = new HtmlDocument()
        doc.LoadHtml html
        doc.DocumentNode.Descendants("a")
            |> List.ofSeq
            |> List.map (fun node -> node.GetAttributeValue("href", null))
            |> List.where (fun item -> not(String.IsNullOrEmpty(item)))
            |> List.map (fun path -> path |> transformPath url)
    with
        | _ -> []
    

//let urls = ["http://titan.dcs.bbk.ac.uk/~kikpef01/testpage.html"]
//
//urls
//|> List.map fetch
//|> Async.Parallel
//|> Async.RunSynchronously
//|> ignore
module crawler.UrlsExtractor

open System
open System.Net
open HtmlAgilityPack

let transformPath (url: string) (path: string) =
    let (canCreate, _) = Uri.TryCreate(path, UriKind.Absolute)
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

let cleanUrl (url: String) =
    url.Trim '#'

let extract (url: string) =            
    try
        let request = WebRequest.Create(Uri url) :?> HttpWebRequest
        request.UserAgent <- Common.USER_AGENT

        let stream = request.GetResponse().GetResponseStream()
        let reader = new IO.StreamReader(stream)
        let html = reader.ReadToEnd()
        
        let doc = new HtmlDocument()
        doc.LoadHtml html
        doc.DocumentNode.Descendants("a")
            |> List.ofSeq
            |> List.map (fun node -> node.GetAttributeValue("href", null))
            |> List.where (fun item -> not(String.IsNullOrEmpty(item)))
            |> List.map (fun path -> path |> transformPath url)
            |> List.map (fun s -> s |> cleanUrl)
    with
        | _ -> []

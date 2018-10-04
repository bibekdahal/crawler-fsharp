module Crawler.UrlValidator
open System
open System.Collections.Generic
open System.Net
open System.Text.RegularExpressions

type Rule = { ruleType: string; path: string; }
let siteRules = Dictionary<string, Rule list>()

// TODO: User Agent

let isValidScheme scheme =
    scheme = Uri.UriSchemeHttp || scheme = Uri.UriSchemeHttps

let regexify (originalStr: string) =
    let str = originalStr.Trim() |> Regex.Escape
    if str.Length = 0 then 
        ".*"
    else 
        "^" + str.Replace("\\*", ".*")
    
let parseRules (uri: Uri) =
    let robotsUri = (new UriBuilder(uri.Scheme, uri.Host, uri.Port, "/robots.txt")).Uri
    
    let req = WebRequest.Create(robotsUri)
    use stream = req.GetResponse().GetResponseStream()
    use reader = new IO.StreamReader(stream)
    let html = reader.ReadToEnd()
    
    let lines =
        Regex.Split(html, @"\r?\n")
            |> Array.toList
            |> List.map (fun l -> l.Trim().ToLower())
            
    let userAgentLength = "User-agent:".Length
    let disallowLength = "Disallow:".Length
    let allowLength = "Allow:".Length
    
    let mutable collectRules = false 
    let mutable rulesStarted = false 
    let rules = new List<Rule>()
    
    lines |> List.iter(fun (line: string) ->
        if collectRules then 
            if line.StartsWith "disallow:" then
                rules.Add { ruleType = "disallow"; path = regexify(line.Substring disallowLength) }
                rulesStarted <- true 
            else if line.StartsWith "allow:" then 
                rules.Add { ruleType = "allow"; path = regexify(line.Substring allowLength) }
            else if rulesStarted && line.StartsWith "user-agent:" then 
                let userAgent = (line.Substring userAgentLength).Trim()
                if userAgent <> "*" then
                    collectRules <- false 
        else if line.StartsWith "user-agent:" then 
            let userAgent = (line.Substring userAgentLength).Trim()
            if userAgent = "*" then 
                collectRules <- true 
                rulesStarted <- false
    )
    
    rules
        |> List.ofSeq
        |> List.sortWith (fun (rule1: Rule) (rule2: Rule) ->
            if rule1.path.Length = rule2.path.Length then 
                if rule1.ruleType = "allow" then -1 else 1
            else 
                rule2.path.Length - rule1.path.Length
                
    )
    
let checkRuleFor (uri: Uri) =
    try
        let path = uri.AbsolutePath
        let key = uri.Scheme + "://" + uri.Host + ":" + (uri.Port |> string)
        
        let rules =
            if siteRules.ContainsKey key then 
                siteRules.[key]
            else
                let newRules = parseRules uri
                siteRules.Add (key, newRules)
                newRules
             
        rules
            |> List.tryFind (fun r -> Regex.Match(path, r.path).Success)
            |> Option.forall (fun r ->
                let allow = not (r.path = "")
                if r.ruleType = "allow" then allow else not allow)
    with
        | _ -> true

let validateUrl (url: string) =
     let (canCreate, uri) = Uri.TryCreate(url, UriKind.Absolute)
     let isValid = canCreate && (isValidScheme uri.Scheme)
     isValid && checkRuleFor uri

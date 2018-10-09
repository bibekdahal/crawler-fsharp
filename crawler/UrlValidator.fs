module crawler.UrlValidator

open System
open System.Collections.Generic
open System.Net
open System.Text.RegularExpressions
open crawler

// Represents a rule in robots.txt
type Rule = { directive: string; path: string; }

// We will cache the rules for each website so that it can be used when the site is crawled again.
let siteRules = Dictionary<string, Rule list>()

// One of the things we want to test if the URL is of supported protocol: http or https
let isValidScheme scheme =
    scheme = Uri.UriSchemeHttp || scheme = Uri.UriSchemeHttps

// We will convert the paths defined in robots.txt into valid regex strings so that we can easily
// match them with actual path later.
let regexify (originalStr: string) =
    let str = originalStr.Trim() |> Regex.Escape
    if str.Length = 0 then 
        ".*"
    else 
        "^" + str.Replace("\\*", ".*")
    
// Connect to robots.txt file of a website and parse the rules into a list.
let parseRules (uri: Uri) =
    try
        // Get the content of robots.txt
        let robotsUri = (new UriBuilder(uri.Scheme, uri.Host, uri.Port, "/robots.txt")).Uri
        let request = WebRequest.Create robotsUri :?> HttpWebRequest
        request.UserAgent <- Common.USER_AGENT

        use stream = request.GetResponse().GetResponseStream()
        use reader = new IO.StreamReader(stream)
        let html = reader.ReadToEnd()
    
        // Split the content into a list of lowercase lines
        let lines = Regex.Split(html, @"\r?\n") |> Array.map (fun l -> l.Trim().ToLower())

        let userAgentLength = "User-agent:".Length
        let disallowLength = "Disallow:".Length
        let allowLength = "Allow:".Length
    
        let mutable collectRules = false 
        let mutable rulesStarted = false 
        let rules = new List<Rule>()
    
        // Collecting the site rules from the lines is basically a finite state machine.
        // The state variables are: rules, collectRules and rulesStarted.
        // We go through each line and change these state variables until all rules are collected.
        lines |> Array.iter(fun (line: string) ->
            if collectRules then 
                if line.StartsWith "disallow:" then
                    rules.Add { directive = "disallow"; path = regexify(line.Substring disallowLength) }
                    rulesStarted <- true 
                else if line.StartsWith "allow:" then 
                    rules.Add { directive = "allow"; path = regexify(line.Substring allowLength) }
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
    
        // Sort the rules by longest path and prefer allow to disallow
        rules
            |> List.ofSeq
            |> List.sortWith (fun (rule1: Rule) (rule2: Rule) ->
                if rule1.path.Length = rule2.path.Length then 
                    if rule1.directive = "allow" then -1 else 1
                else 
                    rule2.path.Length - rule1.path.Length
                
            )
    with
        // If we cannot parse the rules, it probably because there is no proper robots.txt file.
        // In such case, we just assume that there is a rule that disallows nothing.
        | _ -> [{ directive = "disallow"; path = regexify "" }]

// Helper function to check if we can access a given URL for crawling according the site rules.
// It first checks if the site rules are already cached and uses it. If not, it parses the robots.txt
// file and grab the site rules.
let checkRuleFor (uri: Uri) =
    try
        let path = uri.AbsolutePath
        let key = uri.Scheme + "://" + uri.Host + ":" + (uri.Port |> string)
        
        // Either grab the cached rules or get them from robots.txt file
        let rules =
            if siteRules.ContainsKey key then 
                siteRules.[key]
            else
                let newRules = parseRules uri
                siteRules.Add (key, newRules)
                newRules
             
        // Check if a rule exists that disallows this path
        rules
            |> List.tryFind (fun r -> Regex.Match(path, r.path).Success)
            |> Option.forall (fun r ->
                let allow = not (r.path = "")
                if r.directive = "allow" then allow else not allow)
    with
        | _ -> true

// Check if we can access a URL through a simple HEAD request
let canAccess (uri: Uri): bool =
    try
        let request = WebRequest.Create uri :?> HttpWebRequest 
        request.Method <- "HEAD"
        request.UserAgent <- Common.USER_AGENT
        use response = request.GetResponse() :?> HttpWebResponse
        response.Close()
        response.StatusCode = HttpStatusCode.OK
    with
        _ -> false

// Validation of a URL is a combination of validating following items:
// * the format of the input string matching that of a URL (tested by Uri.TryCreate)
// * the protocol of the URL (isValidScheme)
// * whether the URL gives a successful response (canAccess)
// * and whether the URL allows us to crawl (checkRuleFor)
let validateUrl (url: string) =
    let (canCreate, uri) = Uri.TryCreate(url, UriKind.Absolute)
    let isValid = canCreate && (isValidScheme uri.Scheme) && (canAccess uri)
    isValid && checkRuleFor uri

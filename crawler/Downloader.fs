module crawler.Downloader

open System
open System.IO
open System.Net

let downloadUrl (url: string) (outputDir: Option<string>) =
    Console.WriteLine url
    if outputDir.IsSome then
        use webClient = new WebClient()
        webClient.Headers.Add("user-agent", Common.USER_AGENT)

        let dirInfo = Directory.CreateDirectory(outputDir.Value)
        let sanitizedPath = String.Join("_", url.Split(Path.GetInvalidFileNameChars()))
        webClient.DownloadFile(url, Path.Combine(dirInfo.ToString(), sanitizedPath))
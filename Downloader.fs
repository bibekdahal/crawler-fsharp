module Crawler.Downloader
open System
open System.IO
open System.Net

let downloadUrl (url: string) (outputDir: Option<string>) =
    Console.WriteLine url
    if outputDir.IsSome then
        let webClient = new WebClient()
        let dirInfo = Directory.CreateDirectory(outputDir.Value)
        let sanitizedPath = url.Replace("://", "_").Replace("/", "_").Replace(".", "_")
        webClient.DownloadFile(url, Path.Combine(dirInfo.ToString(), sanitizedPath))
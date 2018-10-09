module crawler.Downloader

open System
open System.IO
open System.Net

// A downloader that downloads file from a URL
let downloadUrl (outputDir: Option<string>) =
    // Create the directory if it doesn't exist
    let dirInfo = Directory.CreateDirectory(outputDir.Value)

    // Actual download function that takes a url and downloads the file
    fun (url: string) ->
        // Print the URL that is being downloaded
        Console.WriteLine url
        if outputDir.IsSome then
            use webClient = new WebClient()
            webClient.Headers.Add("user-agent", Common.USER_AGENT)

            // Files are downloaded with same names as the URLs.
            // But certain characters cannot be saved as filename.
            // So sanitize the filename by replacing invalid characters with underscore.
            let sanitizedPath = String.Join("_", url.Split(Path.GetInvalidFileNameChars()))

            // Next, download the file.
            webClient.DownloadFile(url, Path.Combine(dirInfo.ToString(), sanitizedPath))
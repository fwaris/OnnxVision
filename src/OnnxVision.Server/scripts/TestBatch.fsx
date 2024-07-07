#load "Client.fsx"
open System.IO
open System.Diagnostics
open FSharp.Control
open AsyncExts

let PARELLISM = 4 // number of parallel requests which equals the number of models loaded into gpu

let url = "http://localhost:5045/vision/infer"

let folder = @"G:/s/gc"
let imageFiles = 
    Directory.GetFiles(folder, "*.jpeg")
    |>Array.filter(fun f -> FileInfo(f).Length > 1024L) //ignore very small images
printfn "Found %d images" imageFiles.Length


let systemMessage = "You are an AI assistant that helps people find information. Answer questions using a direct style. Do not share more information that the requested by the users."
let userPrompt = "Is the image a technical drawing or does it contain tabular data? Answer with 'yes' or 'no' only."

let testResponse = Client.processImage url (systemMessage,userPrompt,File.ReadAllBytes(imageFiles.[0])) |> Async.RunSynchronously

let timer = Stopwatch()
timer.Start()
let results =
    imageFiles
    |> AsyncSeq.ofSeq
    |> AsyncSeq.mapAsyncParallelThrottled PARELLISM (fun path -> async {
        let img = File.ReadAllBytes(path)
        let! response = Client.processImage url (systemMessage, userPrompt, img) 
        return (path,response)
        })
    |> AsyncSeq.toBlockingSeq
    |> Seq.toList
timer.Stop()
printfn $"{imageFiles.Length} images processed in {timer.Elapsed}"


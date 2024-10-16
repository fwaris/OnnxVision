#load "Client.fsx"
open System.IO
open System.Diagnostics
open FSharp.Control
open AsyncExts
open Plotly.NET
open Plotly.NET.LayoutObjects

let PARELLISM = 4 // number of parallel requests which equals the number of models loaded into gpu

let url = "http://localhost:5045/vision/infer"

let folder = @"G:/s/gc"
let imageFiles = 
    Directory.GetFiles(folder, "*.jpeg")
    |>Array.filter(fun f -> FileInfo(f).Length > 1024L) //ignore very small images
printfn "Found %d images" imageFiles.Length

let plotImageSzDist() = 
    imageFiles 
    |> Array.map(FileInfo) 
    |> Array.map (fun x -> min x.Length 100000L) 
    |> Chart.BoxPlot
    |> Chart.withTraceInfo "bytes"
    |> Chart.withXAxisStyle(TitleText="Image size (bytes)", ShowLine=true)
    |> Chart.withSize(800.,800.)
    |> Chart.withTitle "Image size distribution" 
    |> Chart.show
(*
plotImageSzDist()
*)

let systemMessage = "You are an AI assistant that helps people find information. Answer questions using a direct style. Do not share more information that the requested by the users."
let userPrompt = "Is the image a technical drawing or does it contain tabular data? Answer with 'yes' or 'no' only."

let testResponse = Client.processImage url (systemMessage,userPrompt,File.ReadAllBytes(imageFiles.[0])) |> Async.RunSynchronously
;;
let timer = Stopwatch()
timer.Start()
let results =
    imageFiles
    |> Seq.indexed
    |> AsyncSeq.ofSeq
    |> AsyncSeq.mapAsyncParallelThrottled PARELLISM (fun (i,path) -> async {
        let img = File.ReadAllBytes(path)
        let t1 = Stopwatch()
        t1.Start()
        let! response = Client.processImage url (systemMessage, userPrompt, img) 
        t1.Stop()
        printfn $"Processed {i}/{imageFiles.Length} {path} in {t1.Elapsed.TotalSeconds} sec with response {response}"
        return (path,response)
        })
    |> AsyncSeq.toBlockingSeq
    |> Seq.toList
timer.Stop()
printfn $"{imageFiles.Length} images processed in {timer.Elapsed.TotalMinutes} min"

results |> Seq.map snd |> Seq.map(fun x -> x.Trim().ToLower()) |> Seq.filter (fun x -> x = "yes") |> Seq.length 

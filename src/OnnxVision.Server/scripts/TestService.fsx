#r "nuget: FSharp.SystemTextJson"
open System
open System.IO
open System.Net.Http
open System.Net.Http.Json
open System.Text.Json
open System.Text.Json.Serialization

let serOptions = 
    let o = JsonSerializerOptions(JsonSerializerDefaults.General)
    o.WriteIndented <- false
    o.ReadCommentHandling <- JsonCommentHandling.Skip
    JsonFSharpOptions.Default()
        .WithAllowNullFields(true)
        .WithAllowOverride(true)
        .WithSkippableOptionFields(false)
        .AddToJsonSerializerOptions(o)        
    o

let processImage (url:string) (sysMsg:string, userPrompt:string, img:byte[]) =
    async{
        use client = new HttpClient()
        client.Timeout <- TimeSpan.FromMinutes(5.)
        let value = (sysMsg,userPrompt,img) 
        use! resp = client.PostAsJsonAsync(url, value, options=serOptions) |> Async.AwaitTask
        let! ret = resp.Content.ReadFromJsonAsync(typeof<string>, serOptions) |> Async.AwaitTask
        return (ret :?> string)
    }

let url = "http://localhost:5045/vision/infer"
let imageFile = @"E:\s\genai\00000_120125 4- Port Antenna - DWG_CellMax.pdf.0_0.jpeg"
let img = File.ReadAllBytes(imageFile)

let systemMessage = "You are an AI assistant that helps people find information. Answer questions using a direct style. Do not share more information that the requested by the users."
let userPrompt = "Describe the image in detail"

let response = processImage url (systemMessage, userPrompt, img) |> Async.RunSynchronously

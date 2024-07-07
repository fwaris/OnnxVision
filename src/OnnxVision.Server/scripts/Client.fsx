#r "nuget: FSharp.SystemTextJson"
#r "nuget: FSharp.Control.AsyncSeq"
#load "../AsyncExt.fs"

open System
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
        client.Timeout <- TimeSpan.FromMinutes(10.)
        let value = (sysMsg,userPrompt,img) 
        use! resp = client.PostAsJsonAsync(url, value, options=serOptions) |> Async.AwaitTask
        let! ret = resp.Content.ReadFromJsonAsync(typeof<string>, serOptions) |> Async.AwaitTask
        return (ret :?> string)
    }

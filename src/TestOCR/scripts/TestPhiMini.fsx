#r "nuget: FSharp.SystemTextJson"
#r "nuget: FSharp.Control.AsyncSeq"
#r "nuget: Plotly.NET"
#r "nuget: NtvLibs.zlib.zlibwapi.runtime.win-x64"
#r "nuget: Microsoft.ML.OnnxRuntimeGenAI"
open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.ML.OnnxRuntimeGenAI
open FSharp.Control

let lib = @"C:\Users\fwaris1\.nuget\packages\ntvlibs.zlib.zlibwapi.runtime.win-x64\1.2.3\runtimes\win-x64\native\zlibwapi.dll"
let _ = NativeLibrary.Load(lib)

module Vision =

    let fullPrompt systemMessage userPrompt =
        if systemMessage = "" then
            $"<|user|>{userPrompt}<|end|><|assistant|>"
        else
            $"<|system|>{systemMessage}<|end|><|user|>{userPrompt}<|end|><|assistant|>"

    let infer (model:Model) (prompt:string)  =
        asyncSeq {
            use processor = new MultiModalProcessor(model);
            use tokenizerStream = processor.CreateStream();
            use tknzr = new Tokenizer(model)
            let pr = tknzr.Encode(prompt)
            use generatorParams = new GeneratorParams(model)
            generatorParams.SetSearchOption("max_length", 10000)
            use generator = new Generator(model, generatorParams)
            while not(generator.IsDone()) do
                generator.ComputeLogits()
                generator.GenerateNextToken()
                let word =
                    let ra =generator.GetSequence(0uL)
                    ra.[(ra.Length-1)]
                let decoded = tokenizerStream.Decode(word)
                //printfn "%s" decoded
                yield decoded
        }

let model = new Model(@"C:\s\models\Phi-3.5-mini-instruct-onnx")

let p1 = Vision.fullPrompt "" "What is the capital of France?"

let run p =
    Vision.infer model p
    |> AsyncSeq.iter (fun d -> printfn "%s" d)
    |> Async.Start



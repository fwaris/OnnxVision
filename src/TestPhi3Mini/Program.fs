module TestPhi3Mini
open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.ML.OnnxRuntimeGenAI
open FSharp.Control

[<AutoOpen>]
module VisionInit =
    // Load the native library needed by ONNX
    do
        let p = System.Reflection.Assembly.GetExecutingAssembly().Location
        let p = Path.GetDirectoryName(p)
        //prob need .so for linux
        let p = $@"{p}/runtimes/{RuntimeInformation.RuntimeIdentifier}/native/zlibwapi.dll"
        if not(File.Exists(p)) then
            failwith $"zlibwaip.dll not found at {p}"
        else
            let _ = NativeLibrary.Load(p)
            ()


module Vision =

    let fullPrompt systemMessage userPrompt =
        if systemMessage = "" then
            $"<|user|>{userPrompt}<|end|><|assistant|>"
        else
            $"<|system|>{systemMessage}<|end|><|user|>{userPrompt}<|end|><|assistant|>"

    let infer (model:Model) (prompt:string)  =
        asyncSeq {
            use tknzr = new Tokenizer(model)
            use tokenizerStream = tknzr.CreateStream();
            let pr = tknzr.Encode(prompt)
            use generatorParams = new GeneratorParams(model)
            generatorParams.SetSearchOption("max_length", 1000)
            generatorParams.SetInputSequences(pr)
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

let model = new Model(@"C:\s\models\Phi-3.5-mini-instruct-onnx\cpu_and_mobile\cpu-int4-awq-block-128-acc-level-4")
let text = File.ReadAllText(@"C:\s\legal\docs\ce7d8bec-ee7d-458b-a88e-ef5c62e2849a.PDF.0_0.jpeg.0_1.txt")

let p1 = Vision.fullPrompt "" $"Extract all the numbers in the text ```{text}```"

let run p =
    Vision.infer model p
    |> AsyncSeq.iter (fun d -> printfn "%s" d)

run p1 |> Async.RunSynchronously


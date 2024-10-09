namespace LerUI
open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.ML.OnnxRuntimeGenAI
open FSharp.Control

[<AutoOpen>]
module LLMInit =
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


module LLM =
    open Microsoft.DeepDev
    let model = lazy(new Model(@"C:\s\models\Phi-3.5-mini-instruct-onnx\cpu_and_mobile\cpu-int4-awq-block-128-acc-level-4"))

    let fullPrompt systemMessage userPrompt =
        if systemMessage = "" then
            $"<|user|>{userPrompt}<|end|><|assistant|>"
        else
            $"<|system|>{systemMessage}<|end|><|user|>{userPrompt}<|end|><|assistant|>"

    let infer (cts:System.Threading.CancellationToken) (model:Model) maxLength (prompt:string)  =
        asyncSeq {
            use tknzr = new Tokenizer(model)
            use tokenizerStream = tknzr.CreateStream();
            let pr = tknzr.Encode(prompt)
            use generatorParams = new GeneratorParams(model)
            generatorParams.SetSearchOption("max_length", float maxLength)
            generatorParams.SetInputSequences(pr)
            use generator = new Generator(model, generatorParams)
            while (not (generator.IsDone())) && (not cts.IsCancellationRequested) do
                generator.ComputeLogits()
                generator.GenerateNextToken()
                let word =
                    let ra =generator.GetSequence(0uL)
                    ra.[(ra.Length-1)]
                let decoded = tokenizerStream.Decode(word)
                yield decoded
                do! Async.Sleep 10
        }

    let estimateTokens (prompt:string) =
        async {
            let tokenizer = TokenizerBuilder.CreateByModelNameAsync("gpt-4").GetAwaiter().GetResult();
            let tokens = tokenizer.Encode(prompt, new System.Collections.Generic.HashSet<string>());
            return float tokens.Count
        }

    let systemMessage = ""

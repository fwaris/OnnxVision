namespace OnnxVision.Server

open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.ML.OnnxRuntimeGenAI
open FSharp.Control
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open OnnxVision
open Microsoft.Extensions.Hosting
open System.Threading.Tasks
open OnnxVision.Client

type VisionConfig =
    {
        ModelPath:string
        ModelInstanceCount:int
    }

type VisionRequest = {
    SystemPrompt:string
    Prompt:string
    Image:byte[];
    ReplyChannel:AsyncReplyChannel<unit>
    Dispatch:Model.ServerInitiatedMessages -> unit
}

type VisionMsg =
    | VisionRequest of VisionRequest
    | OnnxModel of (AsyncReplyChannel<Model>)
    | Dispose

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
            $"<|user|><|image_1|>{userPrompt}<|end|><|assistant|>"
        else
            $"<|system|>{systemMessage}<|end|><|user|><|image_1|>{userPrompt}<|end|><|assistant|>"

    let dispatchResponse dispatch (data:AsyncSeq<string>) =
        async {
            let comp = 
                data
                    |> AsyncSeq.bufferByCountAndTime 10 1000
                    |> AsyncSeq.map (fun xs -> if xs.Length > 0 then String.concat "" xs else "")
                    |> AsyncSeq.iter(Client.Model.Srv_TextChunk>>dispatch)
            match! Async.Catch comp with
            | Choice1Of2 _ -> dispatch (Client.Model.Srv_TextDone(None))
            | Choice2Of2 ex ->
                printfn "%s" ex.Message
                dispatch (Client.Model.Srv_TextDone(Some ex.Message))        
        }

    let infer (model:Model) (prompt:string) (image:byte[]) dispatch =
        async {
            let imagePath = Path.GetTempFileName()
            File.WriteAllBytes(imagePath, image)
            let data =
                asyncSeq {
                    use img = Images.Load(imagePath)
                    use processor = new MultiModalProcessor(model);
                    use tokenizerStream = processor.CreateStream();
                    use inputTensors = processor.ProcessImages(prompt, img)
                    File.Delete(imagePath)
                    use generatorParams = new GeneratorParams(model)
                    generatorParams.SetSearchOption("max_length", 10000)
                    generatorParams.SetInputs(inputTensors)
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
            do! dispatchResponse dispatch data
        }
       

    ///Processes incoming requests, serialized to a single model instance.
    /// (note number of model instances is configurable in appSettings.json)
    let modelAgent (logger:ILogger<VisionRequest>) path = MailboxProcessor.Start(fun inbox ->
        let rec loop model = async {
            let! (msg:VisionMsg) = inbox.Receive()
            match msg with
            | VisionRequest req ->
                try
                    let prompt = fullPrompt req.SystemPrompt req.Prompt
                    let! resp = infer model prompt req.Image req.Dispatch
                    req.ReplyChannel.Reply(resp)
                with ex ->
                    logger.LogError(ex, "Error: %s", ex.Message)
                    req.ReplyChannel.Reply()
                return! loop model
            | OnnxModel rc ->
                rc.Reply(model)
                return! loop model
            | Dispose ->
                model.Dispose() //end loop
        }
        let model =
            try new Model(path)
            with ex ->
                logger.LogError(ex, $"Unable to load model from configured path in appSettings.json `{path}`");
                raise ex
        loop model)

    ///routes incoming requests to model agents in a round-robin fashion
    let routerAgent (logger:ILogger<VisionRequest>) (config:VisionConfig) =
        MailboxProcessor.Start(fun inbox ->
            let rec loop (agents:List<MailboxProcessor<VisionMsg>>,i) = async {
                let! (msg:VisionMsg) = inbox.Receive()
                match msg with
                | VisionRequest req ->
                    let i = (i + 1) % config.ModelInstanceCount
                    try
                        let agent = agents.[i]
                        agent.Post msg
                    with ex ->
                        logger.LogError(ex, "Error: %s", ex.Message)
                        req.ReplyChannel.Reply()
                    return! loop (agents,i)
                | OnnxModel _ -> failwith "Model request not valid for this router"
                | Dispose ->
                    agents |> List.iter (fun a -> a.Post Dispose)
            }
            let modelsAgents = [for _ in 1 .. config.ModelInstanceCount -> modelAgent logger config.ModelPath]
            loop (modelsAgents,0))

    let mutable router = Unchecked.defaultof<_>

    ///Initializes the router agent with the specified configuration (if not already initialized)
    let initRouter logger cfg =
        if router = Unchecked.defaultof<_> then
            router <- routerAgent logger cfg

    let disposeRouter() =
        if router = Unchecked.defaultof<_> then
            router.Post Dispose
            router <- Unchecked.defaultof<_>


type VisionConfiguratorService(cfg:IConfiguration, logger:ILogger<VisionRequest>) =
    interface IHostedService with
        member this.StartAsync(cancellationToken) =
            let mcfg =
                {
                    ModelPath = cfg.GetValue<string>("Vision:ModelPath");
                    ModelInstanceCount = cfg.GetValue<int>("Vision:ModelInstanceCount")
                }

            do Vision.initRouter logger mcfg

            Task.CompletedTask

        member this.StopAsync(cancellationToken) =
            Vision.disposeRouter()
            Task.CompletedTask

    interface IDisposable with
        member this.Dispose() =
            Vision.disposeRouter()
            |> ignore
        
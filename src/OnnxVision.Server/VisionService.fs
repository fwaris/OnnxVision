namespace OnnxVision.Server

open System
open System.IO
open System.Runtime.InteropServices
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open OnnxVision
open Microsoft.ML.OnnxRuntimeGenAI

type VisionConfig = 
    {
        ModelPath:string 
        ModelInstanceCount:int
    }

type VisionRequest = {
    SystemPrompt:string
    Prompt:string
    Image:byte[]; 
    ReplyChannel:AsyncReplyChannel<string>
}

type VisionMsg = 
    | VisionRequest of VisionRequest
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

    let infer (model:Model) (prompt:string) (image:byte[]) = 
        async {
            let imagePath = Path.GetTempFileName()
            File.WriteAllBytes(imagePath, image)
            use img = Images.Load(imagePath)
            use processor = new MultiModalProcessor(model);
            use tokenizerStream = processor.CreateStream();
            use inputTensors = processor.ProcessImages(prompt, img)
            File.Delete(imagePath)
            use generatorParams = new GeneratorParams(model)
            generatorParams.SetSearchOption("max_length", 3027)
            generatorParams.SetInputs(inputTensors)
            let resp =
                seq {
                    use generator = new Generator(model, generatorParams)
                    while not(generator.IsDone()) do
                        generator.ComputeLogits()
                        generator.GenerateNextToken()
                        let word = 
                            let ra =generator.GetSequence(0uL)
                            ra.[(ra.Length-1)]
                        yield tokenizerStream.Decode(word)
                }
                |> String.concat ""
            return resp            
        }

    let modelAgent (logger:ILogger<VisionRequest>) path = MailboxProcessor.Start(fun inbox -> 
        let rec loop model = async {          
            let! (msg:VisionMsg) = inbox.Receive()
            match msg with
            | VisionRequest req ->
                try
                    let prompt = fullPrompt req.SystemPrompt req.Prompt
                    let! resp = infer model prompt req.Image            
                    req.ReplyChannel.Reply(resp)
                with ex -> 
                    logger.LogError(ex, "Error: %s", ex.Message)                
                    req.ReplyChannel.Reply(ex.Message)
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
    let routerAgent (logger:ILogger<VisionRequest>) (config:VisionConfig) = 
        MailboxProcessor.Start(fun inbox ->        
            let rec loop (agents:List<MailboxProcessor<VisionMsg>>,i) = async {
                let! (msg:VisionMsg) = inbox.Receive()
                match msg with
                | VisionRequest req ->
                    try
                        let i = i + 1 % config.ModelInstanceCount
                        let agent = agents.[i]
                        agent.Post msg                    
                    with ex -> 
                        logger.LogError(ex, "Error: %s", ex.Message)                
                        req.ReplyChannel.Reply(ex.Message)
                    return! loop (agents,i)
                | Dispose -> 
                    agents |> List.iter (fun a -> a.Post Dispose)                
            }
            let modelsAgents = [for _ in 1 .. config.ModelInstanceCount -> modelAgent logger config.ModelPath]
            loop (modelsAgents,0))

    let mutable router = Unchecked.defaultof<_>

    let initRouter logger cfg = 
        if router = Unchecked.defaultof<_> then
            router <- routerAgent logger cfg
           
    let disposeRouter() =
        if router = Unchecked.defaultof<_> then
            router.Post Dispose
            router <- Unchecked.defaultof<_>

type VisionService(ctx: IRemoteContext, env: IWebHostEnvironment, cfg:IConfiguration, logger:ILogger<VisionRequest>) =
    inherit RemoteHandler<Client.Model.VisionService>()

    let mcfg = 
        {
            ModelPath = cfg.GetValue<string>("Vision:ModelPath"); 
            ModelInstanceCount = cfg.GetValue<int>("Vision:ModelInstanceCount")
        }

    do Vision.initRouter logger mcfg    

    override this.Handler =
        {
            infer = fun (sysMsg:string,userPrompt:string,image:byte[]) -> async {
                try
                    let! resp = Vision.router.PostAndAsyncReply(fun rc -> 
                            VisionRequest
                                {
                                    SystemPrompt=sysMsg
                                    Prompt=userPrompt
                                    Image=image
                                    ReplyChannel=rc
                                })
                    return resp
                with ex -> 
                    logger.LogError(ex, "Error: %s", ex.Message)                   
                    return raise ex
            }
        }

namespace OnnxVision.Server
open System.Threading.Tasks
open FSharp.Control
open Microsoft.AspNetCore.SignalR
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Primitives
open OnnxVision.Client.Model

type ServerHub() =
    inherit Hub()

    static member SendMessage(client:ISingleClientProxy, msg:ServerInitiatedMessages) =
        task {
            return! client.SendAsync(OnnxVision.Client.ClientHub.FromServer,msg)
        }

    member this.FromClient(msg:ClientInitiatedMessages) : Task =
        let cnnId = this.Context.ConnectionId
        let client = this.Clients.Client(cnnId)
        let dispatch msg = ServerHub.SendMessage(client,msg) |> ignore
        task {
            try
                match msg with
                | Clnt_InferImage (sysMsg,userPrompt,imageBytes) ->
                    let prompt = Vision.fullPrompt sysMsg userPrompt
                    let vc (rc) = 
                        VisionRequest 
                            {
                                SystemPrompt=sysMsg
                                Prompt=prompt
                                Image=imageBytes
                                ReplyChannel=rc
                                Dispatch=dispatch
                            }
                    do! Vision.router.PostAndAsyncReply vc
            with ex -> 
                printfn "%s" ex.Message
                dispatch (Srv_Notify $"error {ex.Message}")
        }

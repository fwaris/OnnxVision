module OnnxVision.Client.Update
open System
open Elmish
open OnnxVision.Client.Model
open Bolero
/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

let startInfer (serverDispatch, model) =
    try 
        let p_i =         
            model.prompt
            |> Option.bind (fun p -> 
                model.image 
                |> Option.map (fun i -> p, i))
        match p_i with
        | Some (prompt, image) ->
            let systemMessage = model.systemMessage |> Option.defaultValue ""
            serverDispatch (Clnt_InferImage (systemMessage, prompt, image))
            { model with isInferring = true; startInferTime = DateTime.Now; endInferTime=DateTime.Now; response = None; }, Cmd.none
        | None -> 
            failwith "Prompt and image must be set"
    with ex -> 
        model,Cmd.ofMsg (Exn ex)

let addChunk model chunk = 
    {model with response = model.response |> Option.map(fun r -> r + chunk) }

let finishReponse model msg = 
    {model with isInferring=false; endInferTime = DateTime.Now},
    msg |> Option.map (fun m -> Cmd.ofMsg(Notify m)) |> Option.defaultValue Cmd.none

let update serverDispatch message model =
    match message with
    | SetPage page -> { model with page = page }, Cmd.none
    | SetPrompt prompt -> { model with prompt = prompt }, Cmd.none
    | SetImage image -> { model with image = image; isLoading=false }, Cmd.none
    | SetSystemMessage systemMessage -> { model with systemMessage = systemMessage }, Cmd.none
    | Infer -> startInfer (serverDispatch, model)
    | Exn exn -> model, Cmd.ofMsg (Error exn.Message)
    | Error msg -> { model with error = Some msg; isInferring=false; isLoading=false }, Cmd.none
    | ClearError -> { model with error = None }, Cmd.none
    | Loading fn -> { model with isLoading = true; fileName = Some fn }, Cmd.none
    | Notify msg -> { model with error = Some msg; }, Cmd.none
    | FromServer (Srv_TextChunk txt) -> addChunk model txt, Cmd.none
    | FromServer (Srv_Notify msg) -> model, Cmd.ofMsg (Notify msg)
    | FromServer (Srv_Error msg) -> model, Cmd.ofMsg (Error msg)
    | FromServer (Srv_TextDone msg) -> finishReponse model msg
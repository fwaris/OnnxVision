module OnnxVision.Client.Update
open System
open Elmish
open OnnxVision.Client.Model
open Bolero
/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)


let inferAsync (remote, model) =
    let p_i =         
        model.prompt
        |> Option.bind (fun p -> 
            model.image 
            |> Option.map (fun i -> p, i))
    async {
        match p_i with
        | Some (prompt, image) ->
            try
                let systemMessage = model.systemMessage |> Option.defaultValue ""
                let! response = remote.infer(systemMessage, prompt, image)
                return Some response
            with exn -> 
                return raise exn
        | None -> return failwith "Prompt and image must be set"
    }

let update remote message model =
    match message with
    | SetPage page -> { model with page = page }, Cmd.none
    | SetResponse response -> { model with response = response; endInferTime=DateTime.Now; isInferring=false}, Cmd.none
    | SetPrompt prompt -> { model with prompt = prompt; isInferring=false }, Cmd.none
    | SetImage image -> { model with image = image; isLoading=false }, Cmd.none
    | SetSystemMessage systemMessage -> { model with systemMessage = systemMessage }, Cmd.none
    | Infer -> {model with startInferTime=DateTime.Now; endInferTime=DateTime.Now; isInferring=true},Cmd.OfAsync.either inferAsync (remote, model) SetResponse Error
    | Error exn -> { model with error = Some exn.Message; isInferring=false; isLoading=false }, Cmd.none
    | ClearError -> { model with error = None }, Cmd.none
    | Loading fn -> { model with isLoading = true; fileName = Some fn }, Cmd.none

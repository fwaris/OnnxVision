module OnnxVision.Client.View.Main
open Bolero
open Bolero.Html
open OnnxVision.Client.Model
open Microsoft.AspNetCore.Components.Forms
open System.IO

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

let homePage model dispatch =
    div {
        attr.``class`` "content"

        h4 { "Prompt:" }
        div {
            textarea {
                attr.style "width:75%;"
                attr.rows 3
                on.input (fun e -> dispatch (SetPrompt (Some (string e.Value))))
                model.prompt |> Option.defaultValue "" |> text
            }
        }
        h1 {attr.empty()}
        div {
            attr.``class`` "level"
            div {
                attr.``class`` "level-left"
                div {
                    attr.``class`` "level-item"
                    button {
                        on.click (fun e -> dispatch Infer)
                        attr.disabled (model.isInferring || model.isLoading)
                        attr.``class`` "button is-primary"
                        "Infer"
                    }
                }
                div {
                    attr.``class`` "level-item"
                    label{ 
                            attr.``for`` "image-file"
                            attr.``class`` "input-label"
                            match model.fileName with
                            | Some x -> x
                            | None -> "Choose a file"
                        }
                    comp<InputFile> {
                        attr.id "image-file"
                        attr.accept ".jpeg,.jpg"
                        attr.``class`` "sr-only"
                        attr.disabled (model.isInferring || model.isLoading)
                        attr.callback "OnChange" (fun (e:InputFileChangeEventArgs) -> 
                            dispatch (Loading e.File.Name)
                            async {
                                try
                                    use str = e.File.OpenReadStream(maxAllowedSize = 1024L * 1024L * 10L) 
                                    use ms = new MemoryStream()
                                    do! str.CopyToAsync(ms) |> Async.AwaitTask
                                    ms.Flush()
                                    let bytes = ms.ToArray()
                                    dispatch (SetImage (Some bytes))
                                with ex -> 
                                    dispatch (Error ex)
                            }
                            |> Async.Start)
                        } 
                    }
            }
        }
        h3{ "Response:" }
        div {
            textarea {
                attr.style "width:75%;"
                attr.rows 10
                model.response |> Option.defaultValue "" |> text
            }
        }
        div {
            span {
                "Infer time (sec): "
                $"{(model.endInferTime - model.startInferTime).TotalSeconds}"
            }
        }
    }

let errorNotification errorText closeCallback =
    div {
        attr.``class`` "notification is-warning"
        cond closeCallback <| function
        | None -> empty()
        | Some closeCallback -> button { attr.``class`` "delete"; on.click closeCallback }
        text errorText
    }

let view model dispatch =
    div {
        attr.``class`` "columns"
        div {
            attr.``class`` "column"
            section {
                attr.``class`` "section"
                cond model.page <| function
                | Home -> homePage model dispatch
                div {
                    attr.id "notification-area"
                    cond model.error <| function
                    | None -> empty()
                    | Some err -> errorNotification err (Some (fun _ -> dispatch ClearError))
                }
            }
        }
    }



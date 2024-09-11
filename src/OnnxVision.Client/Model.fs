module OnnxVision.Client.Model

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open Microsoft.AspNetCore.Components.Forms
open FSharp.Control

/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home

/// The Elmish application's model.
type Model =
    {
        page: Page
        systemMessage: string option
        response: string option
        prompt: string option
        image : byte[] option
        error: string option
        startInferTime: DateTime
        endInferTime: DateTime
        isInferring: bool
        isLoading: bool
        fileName: string option
    }

let initModel =
    {
        page = Home
        error = None
        systemMessage = Some "You are an AI assistant that helps people find information. Answer questions using a direct style. Do not share more information that the requested by the users."
        response = None
        prompt = Some "Describe the image in detail."
        image = None
        startInferTime = DateTime.MinValue
        endInferTime = DateTime.MinValue
        isInferring = false
        isLoading = false
        fileName = None
    }

/// Remote service definition.
type VisionService =
    {
        infer: string*string*byte[] -> Async<string> //system message, user prompt, image -> response
        infer2: string*string*byte[] -> Async<AsyncSeq<string>> //system message, user prompt, image -> response
    }
    interface IRemoteService with
        member this.BasePath = "/vision"


/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | SetResponse of string option
    | SetPrompt of string option
    | SetImage of byte[] option
    | SetSystemMessage of string option
    | Infer
    | Error of exn
    | ClearError
    | Loading of string


module rec OnnxVision.Client.Model

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


/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | SetPrompt of string option
    | SetImage of byte[] option
    | SetSystemMessage of string option
    | Infer
    | Exn of exn
    | Error of string
    | Notify of string
    | ClearError
    | Loading of string
    | FromServer of ServerInitiatedMessages

type ClientInitiatedMessages =
    | Clnt_InferImage of string*string*byte[]

type ServerInitiatedMessages =
    | Srv_Notify of string
    | Srv_Error of string
    | Srv_TextChunk of string
    | Srv_TextDone of string option

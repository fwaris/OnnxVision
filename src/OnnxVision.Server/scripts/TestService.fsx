#load "Client.fsx"
open System.IO
let url = "http://localhost:5045/vision/infer"
let imageFile = @"E:\s\genai\00000_120125 4- Port Antenna - DWG_CellMax.pdf.0_0.jpeg"
let img = File.ReadAllBytes(imageFile)

let systemMessage = "You are an AI assistant that helps people find information. Answer questions using a direct style. Do not share more information that the requested by the users."
let userPrompt = "Describe the image in detail"

let response = Client.processImage url (systemMessage, userPrompt, img) |> Async.RunSynchronously


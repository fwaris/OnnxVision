namespace LerUI
open System
open System.IO
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.Platform.Storage

module Toolbars =
    open FSharp.Control
    open System.Threading
    open Avalonia.Threading

    let doOcr storageProvider setInput clearLog appendLog =
        async {
            try
                clearLog()
                let! files = Dialogs.showFileDialog storageProvider "PDF" [ FilePickerFileType("PDF", Patterns = ["*.pdf"]) ]
                let file = files |> Seq.tryHead |> Option.defaultWith (fun () -> failwith "No file selected")
                let rootPath = file.Path.AbsolutePath
                let fileName = file.Name
                appendLog $"Processing {fileName}"
                let dir = System.IO.Path.GetDirectoryName(rootPath)
                Conversion.exportImagesToDiskScaled (Some(255uy,255uy,255uy)) 2.0 rootPath appendLog
                let imgFiles = Directory.GetFiles(dir, $"{fileName}*.jpeg") |> Seq.indexed
                for (i,file) in imgFiles do
                    do! Async.Sleep 100
                    do! OCR.processImage i file appendLog
                Directory.GetFiles(dir, $"{fileName}*.txt")
                |> Seq.sort
                |> Seq.map File.ReadAllText
                |> String.concat "\n"
                |> setInput
                appendLog "Done"
            with ex ->
                appendLog $"Error: {ex.Message}"
        }
        |> Async.Start

    let inputTools storageProvider setInput clearLog appendLog =
        StackPanel.create [
            StackPanel.orientation Layout.Orientation.Horizontal
            StackPanel.horizontalAlignment Layout.HorizontalAlignment.Stretch
            StackPanel.children [
                Button.create [
                    Button.content "Ocr"
                    Button.onClick (fun _ -> doOcr storageProvider setInput clearLog appendLog )
                    Button.horizontalAlignment Layout.HorizontalAlignment.Right
                    Button.verticalAlignment Layout.VerticalAlignment.Center
                ]
            ]
        ]

    let loadModel appendLog =
        async {
            if LLM.model.IsValueCreated |> not then
                appendLog "Loading model..."
                LLM.model.Value |> ignore
                appendLog "Model loaded"
        }

    let startTokens(cts:IWritable<CancellationTokenSource>) prompt maxLength appendToken appendLog =
        async {
            try
                do!
                    LLM.infer cts.Current.Token LLM.model.Value maxLength prompt
                    |> AsyncSeq.iter appendToken
            with ex ->
                appendLog $"Error: {ex.Message}"
        }

    let createPrompt (userInput:IWritable<string>) (llmPrompt:IWritable<string>) =
        let userPrompt = LLM.applyTemplate userInput.Current llmPrompt.Current
        LLM.fullPrompt LLM.systemMessage userPrompt

    let generate (cts:IWritable<CancellationTokenSource>) (userInput:IWritable<string>) (llmPrompt:IWritable<string>) maxLength appendToken clearOutput clearLog appendLog =
        cts.Set (new CancellationTokenSource())
        let comp =
            async {
                do! loadModel appendLog
                clearLog()
                clearOutput()
                appendLog "Running..."
                let p = createPrompt userInput llmPrompt
                if String.IsNullOrWhiteSpace p then
                    appendLog "No prompt provided"
                else
                    do! startTokens cts p maxLength appendToken appendLog
                    if cts.Current = null || cts.Current.IsCancellationRequested then
                        do appendLog "Cancelled"
                    else
                        appendLog "Done"
                Dispatcher.UIThread.InvokeAsync (fun _ -> cts.Set null) |> ignore
            }
        Async.Start(comp)

    let estimateTokens systemMessage (userInput:IWritable<string>) (llmPrompt:IWritable<string>) setInputTokens appendLog =
        async {
            do! loadModel appendLog
            let p = createPrompt userInput llmPrompt
            let! tokens = LLM.estimateTokens p
            let tokens = tokens * 1.5 |> int // 50% buffer as true token count is unknown
            setInputTokens tokens
        }
        |> Async.Start

    let verticalBar() =
        Border.create [
            Border.borderThickness (Thickness 1)
            Border.borderBrush (Media.Colors.BlanchedAlmond.ToString())
            Border.margin(5., 15., 5., 15.)
            Border.horizontalAlignment Layout.HorizontalAlignment.Left
            Border.verticalAlignment Layout.VerticalAlignment.Stretch
        ]

    let llmTools (cts:IWritable<CancellationTokenSource>) userInput llmPrompt (inputTokens:IWritable<int>) (maxOutputLength:IWritable<int>) appendToken clearOutput clearLog appendLog =
        StackPanel.create [
            StackPanel.orientation Layout.Orientation.Horizontal
            StackPanel.horizontalAlignment Layout.HorizontalAlignment.Stretch
            StackPanel.children [
                Button.create [
                    Button.margin (5., 0., 0., 0.)
                    Button.content "Run"
                    Button.isEnabled (cts.Current = null)
                    Button.onClick (fun _ -> generate cts userInput llmPrompt (inputTokens.Current + maxOutputLength.Current) appendToken clearOutput clearLog appendLog  )
                    Button.horizontalAlignment Layout.HorizontalAlignment.Right
                    Button.verticalAlignment Layout.VerticalAlignment.Center
                ]
                Button.create [
                    Button.margin (5., 0., 0., 0.)
                    Button.content "Cancel"
                    Button.isEnabled (cts.Current <> null)
                    Button.onClick (fun _ -> if cts.Current <> null then cts.Current.CancelAsync() |> ignore; appendLog "Cancelling..." )
                    Button.horizontalAlignment Layout.HorizontalAlignment.Right
                    Button.verticalAlignment Layout.VerticalAlignment.Center
                ]
                verticalBar()
                TextBlock.create [
                    TextBlock.text $"Input Tokens (est.): {inputTokens.Current}"
                    TextBlock.horizontalAlignment Layout.HorizontalAlignment.Left
                    TextBlock.verticalAlignment Layout.VerticalAlignment.Center
                ]
                verticalBar()
                TextBlock.create [
                    TextBlock.text "Max Output Length: "
                    TextBlock.horizontalAlignment Layout.HorizontalAlignment.Left
                    TextBlock.verticalAlignment Layout.VerticalAlignment.Center
                ]
                NumericUpDown.create [
                    NumericUpDown.value maxOutputLength.Current
                    NumericUpDown.minimum 1000
                    NumericUpDown.maximum 100000
                    NumericUpDown.onValueChanged (fun v -> maxOutputLength.Set (int v.Value))
                    NumericUpDown.horizontalAlignment Layout.HorizontalAlignment.Right
                    NumericUpDown.verticalAlignment Layout.VerticalAlignment.Center
                ]
            ]
        ]
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

module OcrTools =

    let doOcr storageProvider clearLog appendLog =
        async {
            clearLog()
            let! files = Dialogs.showFileDialog storageProvider "PDF" [ FilePickerFileType("PDF", Patterns = ["*.pdf"]) ]
            let file = files |> Seq.tryHead |> Option.defaultWith (fun () -> failwith "No file selected")
            let rootPath = file.Path.AbsolutePath
            let fileName = file.Name
            appendLog $"Processing {fileName}"
            let dir = System.IO.Path.GetDirectoryName(rootPath)
            Conversion.exportImagesToDiskScaled (Some(255uy,255uy,255uy)) 2.0 rootPath appendLog
            let files = Directory.GetFiles(dir, $"{fileName}*.jpeg") |> Seq.indexed
            for (i,file) in files do
                do! Async.Sleep 100
                do! OCR.processImage i file appendLog
        }
        |> Async.Start

    let inputTools storageProvider (input:IWritable<string>) clearLog appendLog =
        StackPanel.create [
            StackPanel.orientation Layout.Orientation.Horizontal
            StackPanel.horizontalAlignment Layout.HorizontalAlignment.Stretch
            StackPanel.children [
                Button.create [
                    Button.content "Ocr"
                    Button.onClick (fun _ -> doOcr storageProvider clearLog appendLog )
                    Button.horizontalAlignment Layout.HorizontalAlignment.Right
                    Button.verticalAlignment Layout.VerticalAlignment.Center
                ]
            ]
        ]
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

    let doOcr storageProvider (ocrFile:IWritable<string>) appendLog =
        task {
            let! files = Dialogs.showFileDialog storageProvider "PDF" [ FilePickerFileType("PDF", Patterns = ["*.pdf"]) ]
            let file = files |> Seq.tryHead |> Option.defaultWith (fun () -> failwith "No file selected")
            ocrFile.Set file.Name
            let rootPath = file.Path.AbsolutePath
            let fileName = file.Name
            appendLog $"Processing {fileName}"
            let dir = System.IO.Path.GetDirectoryName(rootPath)
            Conversion.exportImagesToDiskScaled (Some(255uy,255uy,255uy)) 2.0 rootPath appendLog
            let files = Directory.GetFiles(dir, $"{fileName}*.jpeg")
            for file in files do
                OCR.processImage file appendLog
        }
        |> ignore

    let inputTools storageProvider (ocrFile:IWritable<string>) (input:IWritable<string>) clearLog appendLog =
        StackPanel.create [
            StackPanel.orientation Layout.Orientation.Horizontal
            StackPanel.horizontalAlignment Layout.HorizontalAlignment.Stretch
            StackPanel.children [
                Button.create [
                    Button.content "Ocr"
                    Button.onClick (fun _ -> clearLog(); doOcr storageProvider ocrFile appendLog )
                    Button.horizontalAlignment Layout.HorizontalAlignment.Right
                    Button.verticalAlignment Layout.VerticalAlignment.Center
                ]
            ]
        ]
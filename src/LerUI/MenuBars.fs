namespace LerUI
open Avalonia
open Avalonia.Controls
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL

module MenuBar =
    open System.IO
    open Avalonia.Platform.Storage
    open Avalonia.Threading
    open System.Collections.Generic

    let create
        (window:Window)
        (ocrFile:IWritable<string>)
        (textFile:IWritable<string>)
        =
        let doOcr (store:IWritable<string>) =
            task {
                let! files = Dialogs.showFileDialog window.StorageProvider "PDF" [ FilePickerFileType("PDF", Patterns = ["*.pdf"]) ]
                match files |> Seq.tryHead with
                | Some file ->
                    store.Set file.Name
                | None -> ()
            }
            |> ignore

        let readText (store:IWritable<string>)  =
            task {
                let! files = Dialogs.showFileDialog window.StorageProvider "Text" [ FilePickerFileType("Text", Patterns= [".txt"]) ]
                match files |> Seq.tryHead with
                | Some file ->
                    store.Set (File.ReadAllText(file.Path.AbsolutePath))
                | None -> ()
            }
            |> ignore


        StackPanel.create [
            StackPanel.orientation Layout.Orientation.Vertical
            StackPanel.verticalAlignment Layout.VerticalAlignment.Top
            StackPanel.margin 5.0
            StackPanel.children [
                Button.create [
                    Button.content "OCR Pdf"
                    Button.onClick (fun _ -> doOcr ocrFile)
                    Button.horizontalAlignment Layout.HorizontalAlignment.Stretch
                ]
                Button.create [
                    Button.content "Import Text From File" (*"←"*)
                    Button.onClick (fun _ -> readText textFile)
                    Button.horizontalAlignment Layout.HorizontalAlignment.Stretch]
                ]
        ]

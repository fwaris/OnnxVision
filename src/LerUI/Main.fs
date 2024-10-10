namespace LerUI
open System
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI
open Avalonia.FuncUI.Elmish
open Avalonia.Threading

#nowarn "57"
open Avalonia.Media
open Avalonia.Controls.Primitives
open System.Threading

[<AbstractClass; Sealed>]
type Views =

    static member main (window:Window)  =
        Component (fun ctx ->
            let llmResponse = ctx.useState ("")
            let llmPrompt = ctx.useState ("Extract the main request for T-Mobile given the legal subpoena below:\n ```{{$text}}```")
            let userInput = ctx.useState ("")
            let maxOutputTokens = ctx.useState (500)
            let inputTokens = ctx.useState (0)
            let canelToken : IWritable<CancellationTokenSource> = ctx.useState (null)
            let log = ctx.useState ("")

            let clearLog () =
                Dispatcher.UIThread.InvokeAsync (fun _ -> log.Set "")
                |> ignore

            let appendLog (msg:string) =
                Dispatcher.UIThread.InvokeAsync(fun _ -> log.Set $"{msg}\n{log.Current}")
                |> ignore

            let setInput (msg:string) =
                Dispatcher.UIThread.InvokeAsync(fun _ -> userInput.Set msg)
                |> ignore

            let clearOutput() =
                Dispatcher.UIThread.InvokeAsync(fun _ -> llmResponse.Set "")
                |> ignore

            let appendToken (msg:string) =
                Dispatcher.UIThread.InvokeAsync(fun _ -> llmResponse.Set (llmResponse.Current + msg))
                |> ignore

            let setInputTokens (tokens:int) =
                Dispatcher.UIThread.InvokeAsync(fun _ -> inputTokens.Set tokens)
                |> ignore

            ctx.useEffect (
                    (fun _ -> Toolbars.estimateTokens LLM.systemMessage userInput llmPrompt setInputTokens appendLog ),
                    [EffectTrigger.AfterChange userInput; EffectTrigger.AfterChange llmPrompt; EffectTrigger.AfterInit])

            let gridCellView col row (store:IWritable<string>) =
                Border.create [
                    Grid.column col
                    Grid.row row
                    Border.borderThickness 2
                    Border.borderBrush (Media.Colors.Black.ToString())
                    Border.margin(2., 2., 2., 2.)
                    Border.child (
                        TextBlock.create [
                            TextBlock.dock Dock.Top
                            TextBlock.fontSize 12.0
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.text store.Current
                        ]
                    )
                ]

            let gridCellViewText col row (store:IWritable<string>) label (toolbar:Types.IView) autoScroll =
                Border.create [
                    Grid.column col
                    Grid.row row
                    Border.margin(5., 5., 5., 5.)
                    Border.borderThickness 2
                    Border.borderBrush (Media.Colors.Black.ToString())
                    Border.child (
                        Grid.create [
                            Grid.columnDefinitions "1*"
                            Grid.rowDefinitions "50,1*"
                            Grid.horizontalAlignment HorizontalAlignment.Stretch
                            Grid.children [
                                StackPanel.create [
                                    StackPanel.orientation Orientation.Horizontal
                                    StackPanel.horizontalAlignment HorizontalAlignment.Stretch
                                    StackPanel.clipToBounds true
                                    StackPanel.children [
                                        TextBlock.create [
                                            TextBlock.text label
                                            TextBlock.textDecorations TextDecorations.Underline
                                            TextBlock.horizontalAlignment HorizontalAlignment.Left
                                            TextBlock.verticalAlignment VerticalAlignment.Center
                                            TextBlock.margin(1., 1., 3., 1.)
                                        ]
                                        toolbar
                                    ]
                                ]
                                TextBox.create [
                                    TextBox.row 1
                                    TextBox.dock Dock.Top
                                    TextBox.fontSize 12.0
                                    TextBox.multiline true
                                    TextBox.acceptsReturn true
                                    TextBox.verticalAlignment VerticalAlignment.Stretch
                                    TextBox.horizontalAlignment HorizontalAlignment.Stretch
                                    TextBox.textAlignment TextAlignment.Left
                                    TextBox.textWrapping TextWrapping.Wrap
                                    TextBox.text store.Current
                                    if autoScroll then TextBox.caretIndex (store.Current.Length)
                                    TextBox.onTextChanged (fun e -> store.Set e)
                                ]
                            ]
                        ]
                    )
                ]

            let logView col =
                Border.create [
                    Grid.column col
                    Border.margin(10., 0., 5., 5.)
                    Border.borderThickness 2
                    //Border.borderBrush (Media.Colors.Black.ToString())
                    Border.child (
                        Grid.create [
                            Grid.columnDefinitions "1*"
                            Grid.rowDefinitions "50,1*"
                            Grid.horizontalAlignment HorizontalAlignment.Stretch
                            Grid.verticalAlignment VerticalAlignment.Stretch
                            Grid.children [
                                TextBlock.create [
                                    TextBlock.text "Log"
                                    TextBlock.textDecorations TextDecorations.Underline
                                    TextBlock.horizontalAlignment HorizontalAlignment.Left
                                    TextBlock.verticalAlignment VerticalAlignment.Center
                                    TextBlock.margin(1., 1., 3., 1.)
                                ]
                                TextBlock.create [
                                    TextBlock.row 1
                                    TextBlock.fontSize 12.0
                                    TextBlock.textWrapping TextWrapping.Wrap
                                    TextBlock.verticalAlignment VerticalAlignment.Stretch
                                    TextBlock.horizontalAlignment HorizontalAlignment.Stretch
                                    TextBlock.verticalScrollBarVisibility ScrollBarVisibility.Auto
                                    TextBlock.text log.Current
                                ]
                            ]
                        ]
                    )
                ]

            let opsView =
                Grid.create [
                    Grid.columnDefinitions "1*"
                    Grid.rowDefinitions "1*,1*,1*"
                    Grid.showGridLines true
                    Grid.margin (5., 5., 5., 5.)
                    Grid.children [
                        gridCellViewText 0 0 llmPrompt "LLM Prompt" (Toolbars.llmTools canelToken userInput llmPrompt inputTokens maxOutputTokens appendToken clearOutput clearLog appendLog) false
                        gridCellViewText 0 1 userInput "User Input" (Toolbars.inputTools window.StorageProvider setInput clearLog appendLog) false
                        //gridCellView 0 2 llmResponse
                        gridCellViewText 0 2 llmResponse "Response" (StackPanel.create []) true
                        GridSplitter.create [
                            Grid.row 1
                            Grid.horizontalAlignment HorizontalAlignment.Stretch
                            Grid.verticalAlignment VerticalAlignment.Top
                            //Grid.width 20.0
                        ]
                        GridSplitter.create [
                            Grid.row 2
                            Grid.horizontalAlignment HorizontalAlignment.Stretch
                            Grid.verticalAlignment VerticalAlignment.Top
                            //Grid.width 20.0
                        ]
                    ]
                ]

            //root view
            DockPanel.create [
                DockPanel.children [
                    Grid.create [
                        Grid.columnDefinitions "2*,1*"
                        Grid.children [
                            opsView
                            logView 1
                            GridSplitter.create [
                                //GridSplitter.width 0.1
                                GridSplitter.margin(0., 10., 0., 10.)
                                GridSplitter.background (Media.Colors.DarkGray.ToString())
                                Grid.column 1
                                Grid.horizontalAlignment HorizontalAlignment.Left
                                Grid.verticalAlignment VerticalAlignment.Stretch
                            ]
                        ]
                    ]
                ]
            ]
        )

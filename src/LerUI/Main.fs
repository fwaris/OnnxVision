﻿namespace LerUI
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

[<AbstractClass; Sealed>]
type Views =

    static member main (window:Window)  =
        Component (fun ctx ->
            let grid = ref Unchecked.defaultof<DataGrid>
            let llmResponse = ctx.useState ("")
            let llmPrompt = ctx.useState ("")
            let userInput = ctx.useState ("")
            let ocrFile = ctx.useState ("")
            let inputFile = ctx.useState ("")
            let log = ctx.useState ("")

            ctx.useEffect ((fun _ -> ctx.forceRender()), [EffectTrigger.AfterChange log])

            let clearLog () = log.Set ""

            let appendLog (msg:string) = log.Set $"{msg}\n{log.Current}"

            let gridCellView col row (store:IWritable<string>) =
                Border.create [
                    Grid.column col
                    Grid.row row
                    Border.background Media.Colors.AliceBlue
                    Border.borderThickness 2
                    Border.borderBrush (Media.Colors.Black.ToString())
                    Border.margin(2., 2., 2., 2.)
                    Border.child (
                        TextBlock.create [
                            TextBlock.dock Dock.Top
                            TextBlock.fontSize 48.0
                            TextBlock.verticalAlignment VerticalAlignment.Center
                            TextBlock.horizontalAlignment HorizontalAlignment.Center
                            TextBlock.text store.Current
                        ]
                    )
                ]

            let gridCellViewText col row (store:IWritable<string>) label (toolbar:Types.IView)=
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
                                    StackPanel.children [
                                        TextBlock.create [
                                            TextBlock.text label
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
                                    TextBox.verticalAlignment VerticalAlignment.Stretch
                                    TextBox.horizontalAlignment HorizontalAlignment.Stretch
                                    TextBox.text store.Current
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
                                    TextBlock.text log.Current
                                    TextBlock.textWrapping TextWrapping.Wrap
                                    TextBlock.verticalAlignment VerticalAlignment.Stretch
                                    TextBlock.horizontalAlignment HorizontalAlignment.Stretch
                                    TextBlock.text log.Current
                                ]
                                // ListBox.create [
                                //     Grid.row 1
                                //     ListBox.verticalAlignment VerticalAlignment.Stretch
                                //     ListBox.horizontalAlignment HorizontalAlignment.Stretch
                                //     ListBox.dataItems log.Current
                                // ]
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
                        gridCellViewText 0 0 userInput "User Input" (OcrTools.inputTools window.StorageProvider ocrFile userInput clearLog appendLog)
                        gridCellViewText 0 1 llmPrompt "LLM Prompt" (Button.create [Button.content "Run"])
                        gridCellView 0 2 llmResponse
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
                        Grid.columnDefinitions "1*,1*"
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

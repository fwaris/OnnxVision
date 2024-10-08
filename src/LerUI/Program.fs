namespace LerUI

open System.Collections.ObjectModel
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data
open Avalonia.FuncUI.Hosts
open Avalonia.Themes.Fluent
open Avalonia.FuncUI
open Avalonia.FuncUI.DSL

type MainWindow() as this =
    inherit HostWindow()
    do
        base.Title <- "LER Test UI"
        base.Width <- 500.0
        base.Height <- 500.0
        this.Content <- Views.main this

type App() =
    inherit Application()

    override this.Initialize() =
        this.Styles.Add (FluentTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Dark
        this.Styles.Load "avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml"

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktopLifetime ->
            desktopLifetime.MainWindow <- MainWindow()

        | _ -> ()

module Program =

    [<EntryPoint>]
    let main(args: string[]) =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .UseSkia()
            .StartWithClassicDesktopLifetime(args)
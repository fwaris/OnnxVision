module OnnxVision.Client.App
open Elmish
open Bolero
open Bolero.Remoting
open OnnxVision.Client.Model
open OnnxVision.Client.Update
open OnnxVision.Client.View.Main

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override _.CssScope = CssScopes.MyApp

    override this.Program =
        let visionService = this.Remote<VisionService>()
        let update = update visionService
        Program.mkProgram (fun _ -> initModel, Cmd.none) update view
        |> Program.withRouter router

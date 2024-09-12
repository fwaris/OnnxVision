module OnnxVision.Client.App
open Elmish
open Bolero
open Bolero.Remoting
open OnnxVision.Client.Model
open OnnxVision.Client.Update
open OnnxVision.Client.View.Main
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.SignalR.Client

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    [<Inject>]
    member val logger:ILoggerProvider = Unchecked.defaultof<_> with get, set


    member val hubConn : HubConnection = Unchecked.defaultof<_> with get, set

   // override _.CssScope = CssScopes.MyApp

    override this.Program =

            //hub connection
        this.hubConn <- ClientHub.connection this.logger this.NavigationManager 
        let clientDispatch msg = this.Dispatch (FromServer msg) 
        let serverDispatch = ClientHub.send this.Dispatch this.hubConn
        let serverCall = ClientHub.call this.hubConn
        this.hubConn.On<ServerInitiatedMessages>(ClientHub.FromServer,clientDispatch) |> ignore

        let update = update serverDispatch
        Program.mkProgram (fun _ -> initModel, Cmd.none) update view
        |> Program.withRouter router

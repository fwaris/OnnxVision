namespace OnnxVision.Client
open System
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Components
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.SignalR.Client
open Microsoft.Extensions.DependencyInjection
open FSharp.Control
open Model

module ClientHub =

    let HubPath = "/hub"
    let FromClient = "FromClient"
    let FromServer = "FromServer"

    let configureSer (o:JsonSerializerOptions)=
        JsonFSharpOptions.Default()
            .WithAllowNullFields(true)
            .WithAllowOverride(true)
            .AddToJsonSerializerOptions(o)
        o


    let retryPolicy = [| TimeSpan(0,0,5); TimeSpan(0,0,10); TimeSpan(0,0,30); TimeSpan(0,0,30) |]
        
    //signalr hub connection that can send/receive messages to/from server
    let connection 
        (loggerProvider: ILoggerProvider) 
        (navMgr:NavigationManager)  
        =
        let hubConnection =
            HubConnectionBuilder()
                .AddJsonProtocol(fun o -> configureSer o.PayloadSerializerOptions |> ignore)
                .WithUrl(navMgr.ToAbsoluteUri(HubPath))
                .WithAutomaticReconnect(retryPolicy)
                .ConfigureLogging(fun logging ->
                    logging.AddProvider(loggerProvider) |> ignore
                )
                .Build()
        (hubConnection.StartAsync()) |> Async.AwaitTask |> Async.Start
        hubConnection

    let reconnect (conn:HubConnection) = 
        task {
            try
                do! conn.StopAsync()
                do! conn.StartAsync()
                printfn "hub reconnected"
            with ex ->
                printfn $"hub reconnect failed {ex.Message}" 
        } |> ignore
        
    let rec private retrySend methodName count (conn:HubConnection) (msg:ClientInitiatedMessages) =
        if count < 7 then
            async {
                printfn $"try resend message {count + 1}"
                try
                    if conn.State = HubConnectionState.Connected then
                        do! conn.SendAsync(methodName,msg) |> Async.AwaitTask
                    else
                        do! Async.Sleep 1000
                        return! retrySend methodName (count+1) conn msg
                with ex ->
                        do! Async.Sleep 1000
                        return! retrySend methodName (count+1) conn msg
            }
        else
            async {
                printfn $"retry limit reached of {count}"
                return ()
            }

    let private _send invokeMethod clientDispatch (conn:HubConnection) (msg:ClientInitiatedMessages) =
        task {
            try
                if conn.State = HubConnectionState.Connected then                    
                    do! conn.SendAsync(invokeMethod,msg)
                else
                    retrySend invokeMethod 0 conn msg |> Async.Start
            with ex ->
                retrySend invokeMethod 0 conn msg |> Async.Start
                clientDispatch (Notify ex.Message)
        }
        |> ignore

    let send clientDispatch (conn:HubConnection) (msg:ClientInitiatedMessages) = 
        _send FromClient clientDispatch conn msg


    let call (conn:HubConnection) (msg:ClientInitiatedMessages) =
        conn.SendAsync(FromClient,msg)


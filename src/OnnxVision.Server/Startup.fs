namespace OnnxVision.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Bolero
open Bolero.Remoting.Server
open Bolero.Server
open OnnxVision
open Bolero.Templating.Server
open Microsoft.AspNetCore.HttpLogging

type Startup() =

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddMvc() |> ignore
        services.AddServerSideBlazor() |> ignore
        services
            .AddSignalR(fun o -> o.MaximumReceiveMessageSize <- 5_000_000)
            .AddJsonProtocol(fun o ->OnnxVision.Client.ClientHub.configureSer o.PayloadSerializerOptions |> ignore) |> ignore

        services
            .AddLogging()
            // .AddHttpLogging(fun o -> 
            //     o.LoggingFields <- HttpLoggingFields.All
            //     o.RequestBodyLogLimit <- 100000
            //     o.ResponseBodyLogLimit <- 100000)
            .AddAuthorization()
            .AddBoleroHost()
#if DEBUG
            .AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../OnnxVision.Client")
#endif
        |> ignore

        services.AddHostedService<VisionConfiguratorService>() |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseWebAssemblyDebugging()

        app
            //.UseHttpLogging()
            .UseAuthentication()
            .UseStaticFiles()
            .UseRouting()
            .UseAuthorization()
            .UseBlazorFrameworkFiles()
            .UseEndpoints(fun endpoints ->
#if DEBUG
                endpoints.UseHotReload()
#endif
                endpoints.MapBoleroRemoting() |> ignore
                endpoints.MapBlazorHub() |> ignore
                endpoints.MapHub<ServerHub>(OnnxVision.Client.ClientHub.HubPath) |> ignore
                endpoints.MapFallbackToBolero(Index.page) |> ignore)     
        |> ignore


module Program =

    [<EntryPoint>]
    let main args =
        let app = 
                WebHost
                    .CreateDefaultBuilder(args)            
                    .UseStaticWebAssets()
                    .UseStartup<Startup>()
                    .Build()
        app.Run()
        0

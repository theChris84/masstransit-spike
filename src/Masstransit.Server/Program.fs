module Masstransit.Server.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open FSharp.Control.Tasks.V2.ContextInsensitive

// ---------------------------------
// Models
// ---------------------------------

type Message =
    {
        Text : string
    }

// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "Masstransit.Server" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "Masstransit.Server" ]

    let index (model : Message) =
        [
            partial()
            p [] [ encodedText model.Text ]
        ] |> layout

// ---------------------------------
// Web app
// ---------------------------------

let indexHandler (name : string) =
    let greetings = sprintf "Hello %s, from Giraffe!" name
    let model     = { Text = greetings }
    let view      = Views.index model
    htmlView view
    
let apiVersion : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task{
            let version = Version (1, 0, 0)
            let apiVersionText = sprintf "Api Version: %A" version 
            return! text apiVersionText next ctx
        }
          


 
let handler1 : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        ctx.WriteTextAsync "Hello World"
        
let handler2 (name: string, age: int) =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
            sprintf "Hello %s you are %i years old!" name age
            |> ctx.WriteTextAsync

(*
let endpoints =
    [
        GET [  route "/" (text "Hello World") ] 
        GET [  routef "/%s/%i" handler2 ]
        subRoute "/sub" [
            // Not specifying a http verb means it will listen to all verbs
            route "/test" handler1
        ]
    ]
*)
(*    
let endpoints =
    [ subRoute "/foo" [ GET [ route "/bar" (text "Aloha!") ] ]
      GET [ route "/" (text "Hello World") ]
      GET_HEAD [ route "/foo" (text "Bar")
                 route "/x" (text "y")
                 route "/abc" (text "def") ]
      // Not specifying a http verb means it will listen to all verbs
      subRoute "/sub" [ route "/test" (text "This is a Test") ] ]
*)


let endpoints =
    choose [
        route "/"           >=> indexHandler "World"
        route "/version"    >=> apiVersion
        GET >=>
            choose [
                routef "/hello/%s" indexHandler
            ]
        setStatusCode 404   >=> text "Not Found" ]


// ---------------------------------
// Error h
// ---------------------------------
// Error handler
// ---------------------------------

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

// ---------------------------------
// Config and Main
// ---------------------------------

let configureCors (builder : CorsPolicyBuilder) =
    builder
        .WithOrigins(
            "http://localhost:5000",
            "https://localhost:5001")
       .AllowAnyMethod()
       .AllowAnyHeader()
       |> ignore

let configureServices (services : IServiceCollection) =
    services
        .AddCors()
        .AddRouting()
        .AddOpenApiDocument( fun opt -> opt.Title <- "MassTransit Server Rest API" )
        .AddControllers()
    |> ignore

let configureApp (app : IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    (match env.IsDevelopment() with
    | true  ->
        app.UseDeveloperExceptionPage()
    | false ->
        app .UseGiraffeErrorHandler(errorHandler)
            .UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseRouting()
        .UseOpenApi()
        .UseSwaggerUi3()
        .UseReDoc()
        .UseGiraffe endpoints
    |> ignore

let configureLogging (builder : ILoggingBuilder) =
    builder
        .AddConsole()
        .AddDebug()
    |> ignore

[<EntryPoint>]
let main args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot     = Path.Combine(contentRoot, "WebRoot")
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            fun webHostBuilder ->
                webHostBuilder
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> configureApp)
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                    |> ignore)
        .Build()
        .Run()
    0
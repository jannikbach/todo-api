module TodoApi.Web.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open TodoApi.Web.Models
open TodoApi.Web.TodoHandlers.TodoHandlers
open TodoApi.Core
open TodoApi.Application
open TodoApi.Infrastructure
open Microsoft.OpenApi.Models


// ---------------------------------
// Views
// ---------------------------------

module Views =
    open Giraffe.ViewEngine

    let layout (content: XmlNode list) =
        html [] [
            head [] [
                title []  [ encodedText "TodoApi.web" ]
                link [ _rel  "stylesheet"
                       _type "text/css"
                       _href "/main.css" ]
            ]
            body [] content
        ]

    let partial () =
        h1 [] [ encodedText "TodoApi.web" ]

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
    let model: Message = { Text = greetings }
    let view      = Views.index model
    htmlView view

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> indexHandler "todo"
        //         routef "/hello/%s" indexHandler
        //         route "/todo" >=> getTodos //header um alle, nur erledigt, oder noch offen zu bekommen?
        //         routef "/todo/%O" getTodoById 
        ]
        POST >=>
            choose [
                routef "/todo/%O" updateTodoById
                route "/todo" >=> createTodo
            ]
        // DELETE >=>
        //     choose [
        //         routef "/todo/%O" deleteTotoById
        //     ]
        setStatusCode 404 >=> text "Not Found" ]

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
        .UseSwagger()
        .UseSwaggerUI(fun options -> // UI for testing at /swagger
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo API v1")
        )
        .UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let baseDir = Directory.GetCurrentDirectory()
    let todoFilePath = Path.GetFullPath(Path.Combine(baseDir, "../TodoApi.Infrastructure/Data/todo.json"))
    
    services.AddScoped<ITodoRepository>(fun _ -> TodoRepository(todoFilePath)) |> ignore
    services.AddScoped<ITodoService, TodoService>() |> ignore
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddControllers() |> ignore
    services.AddEndpointsApiExplorer() |> ignore  // Enables endpoint documentation
    services.AddSwaggerGen(fun options ->
        options.SwaggerDoc("v1", OpenApiInfo(Title = "Todo API", Version = "v1"))
    ) |> ignore

let configureLogging (builder : ILoggingBuilder) =
    // Set a logging filter (optional)
    let filter (l : LogLevel) = l.Equals LogLevel.Error

    // Configure the logging factory
    builder.AddFilter(filter) // Optional filter
           .AddConsole()      // Set up the Console logger
           .AddDebug()        // Set up the Debug logger
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
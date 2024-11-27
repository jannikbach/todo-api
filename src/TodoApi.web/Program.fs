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
open Giraffe.OpenApi
open Giraffe.EndpointRouting
open Giraffe.HttpStatusCodeHandlers.RequestErrors
open Swashbuckle.AspNetCore.Swagger
open TodoApi.Core.Model
open TodoApi.Infrastructure
open TodoApi.Web.Models
open TodoApi.Web.TodoHandlers.TodoHandlers
open TodoApi.Core
open TodoApi.Application
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

let notFoundHandler: HttpHandler = notFound (text "Not Found")

let webApp =
      [
        route "/" (redirectTo false "/swagger/index.html")
        GET [
                route "/"  (redirectTo false "/swagger/index.html")
                routef "/hello/%s" indexHandler |> addOpenApiSimple<string, string>
                route "/todos" getTodos |> addOpenApiSimple<unit, Todo list>
                routef "/todos/%O" getTodoById |> addOpenApiSimple<Guid, Todo>
        ]
        POST [
            route "/todos" postTodo |> addOpenApiSimple<Todo, Todo>
        ]
        DELETE [
            routef "/todos/%O" deleteTotoById |> addOpenApiSimple<Guid, string>
        ]
      ]

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

let configureApp (app: IApplicationBuilder) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    
    // Use developer exception page in development, or a custom error handler in production
    if env.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore
    else
        app.UseGiraffeErrorHandler(errorHandler) |> ignore
    
    app
        .UseHttpsRedirection() // Redirect HTTP to HTTPS
        .UseCors(configureCors) // Configure CORS
        .UseStaticFiles() // Serve static files
        .UseRouting() // Adds routing capabilities
        .UseSwagger() // Enables Swagger middleware for generating OpenAPI docs
        .UseSwaggerUI() |> ignore
    app.UseGiraffe(webApp)
        .UseGiraffe(notFoundHandler)|> ignore // Swagger dependencies // Adds Giraffe's route handling middleware

let configureServices (services : IServiceCollection) =
    let baseDir = Directory.GetCurrentDirectory()
    let todoFilePath = Path.GetFullPath(Path.Combine(baseDir, "../TodoApi.Infrastructure/Data/todo.json"))
    
    services
        .AddHttpClient()
        .AddCors()
        .AddRouting()
        .AddGiraffe()
        .AddEndpointsApiExplorer()
        .AddSwaggerGen(fun option ->
            option.SwaggerDoc("v1", OpenApiInfo(Title = "Todo API", Version = "v1")))
    |> ignore
        
    services.AddScoped<ITodoRepository>(fun _ -> TodoFsRepository(todoFilePath)) |> ignore
    services.AddScoped<ITodoService, TodoService>() |> ignore
    services.BuildServiceProvider()
        .GetServices<ISwaggerProvider>()
        |> Seq.iter (fun service -> printfn "SwaggerProvider registered: %A" service) |> ignore
    services.AddControllers() |> ignore

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
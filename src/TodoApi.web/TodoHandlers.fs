module TodoApi.Web.TodoHandlers

open System
open Microsoft.Extensions.Logging
open TodoApi.Web.Models
open TodoApi.Application
open Newtonsoft.Json
open System.IO
open Microsoft.AspNetCore.Http


let createGuid (input: string) =
    try
        Guid(input) // Parse the string to a Guid
    with
    | :? FormatException -> 
        printfn "Invalid GUID format: %s" input
        Guid.Empty // Return an empty Guid if the format is invalid

type OptionConverter() =
    inherit JsonConverter()

    override _.CanConvert(objectType) =
        objectType.IsGenericType && objectType.GetGenericTypeDefinition() = typedefof<option<_>>

    override _.WriteJson(writer, value, serializer) =
        if value = null then
            writer.WriteNull()
        else
            let valueType = value.GetType()
            let underlyingValue = valueType.GetProperty("Value").GetValue(value, null)
            serializer.Serialize(writer, underlyingValue)

    override _.ReadJson(reader, objectType, existingValue, serializer) =
        if reader.TokenType = JsonToken.Null then
            null
        else
            let innerType = objectType.GetGenericArguments().[0]
            let value = serializer.Deserialize(reader, innerType)
            let someConstructor = typedefof<option<_>>.MakeGenericType(innerType).GetConstructor([| innerType |])
            someConstructor.Invoke([| value |])

// A helper for custom JSON serialization/deserialization
module CustomJson =

    let settings =
        let s = JsonSerializerSettings()
        s.ContractResolver <- Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver() // camelCase
        s.Converters.Add(OptionConverter()) // Handle F# option types
        s

    let serialize obj =
        JsonConvert.SerializeObject(obj, settings)

    let deserialize<'T> (json: string) =
        JsonConvert.DeserializeObject<'T>(json, settings)

    let deserializeFromStreamAsync<'T> (stream: Stream) =
        task {
            use reader = new StreamReader(stream)
            let! json = reader.ReadToEndAsync()
            return deserialize<'T> json
        }

module TodoHandlers =
    
    open Microsoft.AspNetCore.Http
    open Giraffe
    

    [<CLIMutable>]
    type TodoInput =
        {
            Id: Guid option
            Text: string option
            IsCompleted: bool option
        }

    
    let handleGetHello: HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let response = {
                    Text = "Hello world, from TodoApi!"
                }
                return! json response next ctx
            }
            
    let getTodos: HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            let logger = ctx.GetLogger<ILogger>()
            task {
                let todoService = ctx.GetService<ITodoService>()
                let! result = todoService.GetAllTodos()
                match result with
                | Ok todos -> return! json todos next ctx
                | Error err -> return! json (sprintf "Error: %A" err) next ctx
            }
            
    let getTodoById (Id: Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let todoService = ctx.GetService<ITodoService>()
                let! result = todoService.GetTodo(Id)
                match result with
                | Ok todo -> return! json todo next ctx
                | Error err -> return! json (sprintf "Error: %A" err) next ctx
            }
            
    let postTodo : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                try
                    let! input = CustomJson.deserializeFromStreamAsync<TodoInput>(ctx.Request.Body)

                    let todoService = ctx.GetService<ITodoService>()
                    
                    match input.Id, input.Text, input.IsCompleted with
                    | Some id, Some text, Some isCompleted ->
                        let! result = todoService.UpdateTodo {Id = id; Text = text; IsCompleted = isCompleted} 
                        match result with
                        | Ok todo -> return! json todo next ctx
                        | Error err -> return! json (sprintf "Error: %A" err) next ctx
                        
                    | Some id, None, Some isCompleted ->
                        if isCompleted then
                            let! result = todoService.CompleteTodo id
                            match result with
                                | Ok todo -> return! json todo next ctx
                                | Error err -> return! json (sprintf "Error: %A" err) next ctx
                        else
                            let! result = todoService.DeCompleteTodo id
                            match result with
                                | Ok todo -> return! json todo next ctx
                                | Error err -> return! json (sprintf "Error: %A" err) next ctx
                            
                    | Some id, Some text, None ->
                        let! result = todoService.UpdateText id text
                        match result with
                        | Ok todo -> return! json todo next ctx
                        | Error err -> return! json (sprintf "Error: %A" err) next ctx
                        
                    | Some id, None, None ->
                        let! result = todoService.GetTodo id 
                        match result with
                        | Ok todo -> return! json todo next ctx
                        | Error err -> return! json (sprintf "Error: %A" err) next ctx
                        
                    | None, Some text, Some isCompleted ->
                        let! result = todoService.CreateTodo text
                        match result with
                        | Ok created ->
                            let! update = todoService.UpdateTodo({Id = created.Id; Text = created.Text; IsCompleted = isCompleted})
                            return! json update next ctx
                        | Error err -> return! json (sprintf "Error: %A" err) next ctx
                        
                    | None, Some text, None ->
                        let! result = todoService.CreateTodo text
                        match result with
                        | Ok created ->
                            return! json created next ctx
                        | Error err -> return! json (sprintf "Error: %A" err) next ctx
                        
                    | _, _, _ -> return! RequestErrors.badRequest (text "invalid argument set") next ctx
                with
                | :? JsonSerializationException as ex ->
                    // Handle deserialization errors
                    return! RequestErrors.badRequest (text $"Invalid JSON: {ex.Message}") next ctx
                | ex ->
                    return! ServerErrors.internalError (text ex.Message) next ctx
            }
            
    let deleteTotoById (id: Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let todoService = ctx.GetService<ITodoService>()
                let! result = todoService.RemoveTodo(id)
                match result with
                | Ok _ -> return! next ctx
                | Error err -> return! json (sprintf "Error: %A" err) next ctx
            }
           
    let createTodo : HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! description = ctx.BindModelAsync<Message>()
            let todoService = ctx.GetService<ITodoService>()
            let! result = todoService.CreateTodo description.Text
            match result with
            | Ok todo -> return! text $"Todo {todo.Id} created" next ctx
            | Error err -> return! json (sprintf "Error: %A" err) next ctx
        }



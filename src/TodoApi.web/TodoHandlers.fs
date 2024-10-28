namespace TodoApi.Web.TodoHandlers

open System
open Microsoft.Extensions.Logging
open TodoApi.Web.Models
open TodoApi.Application

module TodoHandlers =
    
    open Microsoft.AspNetCore.Http
    open Giraffe
    
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
                let response = {
                    Text = "Todo, Todo, Todo,..."
                }
                logger.LogInformation("Todo, Todo, Todo,...")
                return! json response next ctx
            }
            
    let getTodoById (id: Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let response = {
                    Text = $"{id}"
                }
                return! json response next ctx
            }
            
    let updateTodoById (id: Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let response = {
                    Text = $"{id}"
                }
                return! json response next ctx
            }
            
    let deleteTotoById (id: Guid) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let response = {
                    Text = "deleted"
                }
                return! json response next ctx
            }
            
    let createTodo : HttpHandler =
        fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            let! description = ctx.BindModelAsync<Message>()
            let todoService = ctx.GetService<ITodoService>()
            let! result = todoService.CreateTodo description.Text
            match result with
            | Ok _ -> return! text "Todo created" next ctx
            | Error err -> return! json (sprintf "Error: %A" err) next ctx
        }



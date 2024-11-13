namespace TodoApi.Infrastructure

open TodoApi.Core
open TodoApi.Core.Errors
open TodoApi.Core.Model
open System
open System.IO
open Newtonsoft.Json

type TodoRepository (filePath: string) =
    interface ITodoRepository with
        member _.AddTodo (todo : Todo) : Async<Result<Todo, AppError>>=
            async {
                try
                    let todos =
                        if File.Exists(filePath) then
                            let json = File.ReadAllText(filePath)
                            JsonConvert.DeserializeObject<Todo list>(json)
                        else
                            []
                    let updatedTodos = todos @ [todo]
                    let updatedJson = JsonConvert.SerializeObject(updatedTodos, Formatting.Indented)
                    File.WriteAllText(filePath, updatedJson)
                    return Ok todo
                with
                | ex -> return TodoError ($"Failed to add todo: {ex.Message}") |> Error
            }

        member _.GetAllTodos() : Async<Result<Todo list, AppError>> =
            async {
                try
                    let todos =
                        if File.Exists(filePath) then
                            let json = File.ReadAllText(filePath)
                            JsonConvert.DeserializeObject<Todo list>(json)
                        else
                            []
                    return Ok todos
                with
                | ex -> return TodoError ($"Failed to load todos: {ex.Message}") |> Error
            }

        member this.GetTodoById(Id: Guid) = 
            async {
                try
                    let todos =
                        if File.Exists(filePath) then
                            let json = File.ReadAllText(filePath)
                            JsonConvert.DeserializeObject<Todo list>(json)
                        else
                            []
                    let todo = List.find (_.Id.Equals(Id)) todos
                    return Ok todo
                with
                | ex -> return TodoError ($"Failed to load todo {Id}: {ex.Message}") |> Error
            }
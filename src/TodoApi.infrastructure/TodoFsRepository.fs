module TodoApi.Infrastructure

open TodoApi.Core
open TodoApi.Core.Errors
open TodoApi.Core.Model
open System
open System.IO
open Newtonsoft.Json


let loadTodos (filePath: string) : Todo list =
    if File.Exists(filePath) then
        let json = File.ReadAllText(filePath)
        JsonConvert.DeserializeObject<Todo list>(json)
    else
        []
let saveTodos (filePath: string) (todos: Todo list) =
    let json = JsonConvert.SerializeObject(todos, Formatting.Indented)
    File.WriteAllText(filePath, json)

type TodoFsRepository (filePath: string) =
    let loadTodos () = loadTodos filePath

    let saveTodos (todos: Todo list) = saveTodos filePath todos
    
    interface ITodoRepository with
        member _.GetAllTodos() : Async<Result<Todo list, AppError>> =
            async {
                try
                    let todos = loadTodos()
                    return Ok todos
                with
                | ex -> return TodoError ($"Failed to load todos: {ex.Message}") |> Error
            }

        member _.AddTodo (todo : Todo) : Async<Result<Todo, AppError>>=
            async {
                try
                    loadTodos () 
                        |> List.append [todo]
                        |> saveTodos
                    return Ok todo
                with
                | ex -> return TodoError ($"Failed to add todo: {ex.Message}") |> Error
            }
            
        member this.GetTodoById(Id: Guid) = 
            async {
                try
                    let todo = loadTodos()
                               |> List.find (_.Id.Equals(Id))
                    return Ok todo
                with
                | ex -> return TodoError ($"Failed to load todo {Id}: {ex.Message}") |> Error
            }

        member this.DeleteTodoById(id: Guid) =
            async {
                try
                    loadTodos () 
                    |> List.filter (fun x -> x.Id <> id)
                    |> saveTodos
                    return Ok ()
                with
                | ex -> return TodoError ($"Failed to delete todo {id}: {ex.Message}") |> Error
            }

        member this.UpdateTodo(todo: Todo) =
            async {
                try
                    loadTodos () 
                        |> List.filter (fun x -> x.Id <> todo.Id)
                        |> List.append [todo]
                        |> saveTodos
                    return Ok todo
                with
                | ex -> return TodoError ($"Failed to update todo {todo.Id}: {ex.Message}") |> Error
            }
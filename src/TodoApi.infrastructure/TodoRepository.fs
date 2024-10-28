namespace TodoApi.Infrastructure

open TodoApi.Core
open TodoApi.Core.Errors
open TodoApi.Core.Model
open System.IO
open Newtonsoft.Json

type TodoRepository (filePath: string) =
    interface ITodoRepository with
        member _.AddTodo (todo : Todo) : Async<Result<Unit, AppError>>=
            async {
                try
                    let todos =
                        if File.Exists(filePath) then
                            let json = File.ReadAllText(filePath)
                            let maybeTodos = JsonConvert.DeserializeObject<Todo list option>(json)
                            Option.defaultValue [] maybeTodos
                        else
                            []
                    let updatedTodos = todos @ [todo]
                    let updatedJson = JsonConvert.SerializeObject(updatedTodos, Formatting.Indented)
                    File.WriteAllText(filePath, updatedJson)
                    return Ok ()
                with
                | ex -> return TodoError ($"Failed to add todo: {ex.Message}") |> Error
            }


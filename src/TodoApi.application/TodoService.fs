namespace TodoApi.Application

open System
open TodoApi.Application
open TodoApi.Core
open TodoApi.Core.Model
open TodoApi.Core.Errors



type TodoService(repository: ITodoRepository) =
    interface ITodoService with
        member _.CreateTodo(description: string) : Async<Result<Todo, AppError>> =
            let todo = {
                Id = Guid.NewGuid()
                Text = description
                IsCompleted = false
            }
            repository.AddTodo todo

        member _.GetAllTodos() : Async<Result<Todo list, AppError>> =
            repository.GetAllTodos()

        member this.GetTodo(id :Guid) =
            repository.GetTodoById(id)

        member this.RemoveTodo(id: Guid) =
            repository.DeleteTodoById(id)

        
        
        member this.UpdateText (id: Guid) (text: string) =
            async {
                let! result = repository.GetTodoById(id)
                match result with
                | Ok todo ->
                    return! repository.UpdateTodo({ todo with Text = text })
                | Error err ->
                    return Error err
            }

        member this.CompleteTodo (id: Guid) = 
            async {
                let! result = repository.GetTodoById(id)
                match result with
                | Ok todo ->
                    return! repository.UpdateTodo({ todo with IsCompleted = true })
                | Error err ->
                    return Error err
            }
            
        member this.DeCompleteTodo(id: Guid) =
            async {
                let! result = repository.GetTodoById(id)
                match result with
                | Ok todo ->
                    return! repository.UpdateTodo({ todo with IsCompleted = false })
                | Error err ->
                    return Error err
            }

        member this.GetUncompletedTodos() =
            async {
                let! result = repository.GetAllTodos()
                match result with
                | Ok todos ->
                    let completed = List.filter (fun x -> not x.IsCompleted) todos
                    return Ok completed
                | Error err -> return Error err
            }

        member this.GetCompletedTodos() =
            async {
                let! result = repository.GetAllTodos()
                match result with
                | Ok todos ->
                    let completed = List.filter (_.IsCompleted) todos
                    return Ok completed
                | Error err -> return Error err
            }

        member this.UpdateTodo(todo: Todo) =
            repository.UpdateTodo(todo)
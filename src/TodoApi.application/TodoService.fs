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

        member this.GetTodoById(Id :Guid) =
            repository.GetTodoById(Id)


namespace TodoApi.Application

open System
open TodoApi.Application
open TodoApi.Core
open TodoApi.Core.Model
open TodoApi.Core.Errors



type TodoService(repository: ITodoRepository) =
    interface ITodoService with
        member this.CreateTodo(description: string) : Async<Result<Unit, AppError>> =
            let todo = {
                Id = Guid.NewGuid()
                Text = description
                IsCompleted = false
            }
            repository.AddTodo todo


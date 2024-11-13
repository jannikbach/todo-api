namespace  TodoApi.Core

open System
open TodoApi.Core.Model
open TodoApi.Core.Errors


type ITodoRepository =
    abstract member AddTodo : Todo -> Async<Result<Todo, AppError>>
    // abstract member DeleteTodoById : Guid -> Async<Result<Unit, AppError>>
    abstract member GetTodoById : Guid -> Async<Result<Todo, AppError>>
    abstract member GetAllTodos: unit -> Async<Result<Todo list, AppError>>
    // abstract member UpdateTodo: Todo -> Async<Result<Unit, AppError>>


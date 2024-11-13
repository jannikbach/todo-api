namespace TodoApi.Application

open TodoApi.Core.Errors
open TodoApi.Core.Model
open System

type ITodoService =
    abstract member CreateTodo : string -> Async<Result<Todo, AppError>>
    // abstract member DeleteTodoById : Guid -> Async<Result<Unit, AppError>>
    abstract member GetTodoById : Guid -> Async<Result<Todo, AppError>>
    abstract member GetAllTodos: unit -> Async<Result<Todo list, AppError>>
    // abstract member UpdateTodo: Todo -> Async<Result<Unit, AppError>>
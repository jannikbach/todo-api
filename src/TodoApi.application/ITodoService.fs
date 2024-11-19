namespace TodoApi.Application

open TodoApi.Core.Errors
open TodoApi.Core.Model
open System

type ITodoService =
    abstract member CreateTodo : string -> Async<Result<Todo, AppError>>
    abstract member RemoveTodo : Guid -> Async<Result<Unit, AppError>>
    abstract member GetTodo : Guid -> Async<Result<Todo, AppError>>
    abstract member GetAllTodos: unit -> Async<Result<Todo list, AppError>>
    abstract member GetCompletedTodos: unit -> Async<Result<Todo list, AppError>>
    abstract member GetUncompletedTodos: unit -> Async<Result<Todo list, AppError>>
    abstract member UpdateText: Guid -> string -> Async<Result<Todo, AppError>>
    abstract member CompleteTodo: Guid -> Async<Result<Todo, AppError>>
    abstract member DeCompleteTodo: Guid -> Async<Result<Todo, AppError>>
    abstract member UpdateTodo: Todo -> Async<Result<Todo, AppError>>

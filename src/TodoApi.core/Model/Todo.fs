namespace TodoApi.Core.Model
open System

type Todo = {
        Id: Guid
        Text: string
        IsCompleted: bool
    }

module TodoApi.Core.Errors

open System

type AppError =
    | TodoError of string
    | AggregateError of AppError list
    | TodoRepositoryError of string

let rec describeError =
    function
    | TodoError error -> $"Todo Error: {error}"
    | AggregateError errors ->
        let errorMessages =
            errors
            |> List.map describeError
            |> fun e -> String.Join("\n", e)
        $"""Multiple Errors Occured: \n{errorMessages}"""
    | TodoRepositoryError error -> $"TodoRepositoryError Error: {error}"
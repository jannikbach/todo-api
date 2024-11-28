module TodoApi.IntegrationTests

open System
open System.IO
open Xunit
open FsUnit
open TodoApi.Core.Model
open TodoApi.Core
open TodoApi.Infrastructure
open TodoApi.Core.Errors

let createTempFile () =
    let tempFile = Path.GetTempFileName()
    File.WriteAllText(tempFile, "[]") // Start with an empty todos list
    tempFile

let deleteTempFile (filePath: string) =
    if File.Exists(filePath) then
        File.Delete(filePath)

[<Fact>]
let ``GetAllTodos should return an empty list when file is empty`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        let result = repo.GetAllTodos() |> Async.RunSynchronously
        match result with
            | Ok todos -> 
                todos.Length |> should equal 0
            | Error _ -> failwith "Test failed"
    
    finally
        deleteTempFile filePath

[<Fact>]
let ``AddTodo should add a todo to the list`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        let todo = { Id = Guid.NewGuid(); Text = "Test Todo"; IsCompleted = false }
        let addResult = repo.AddTodo todo |> Async.RunSynchronously
        match addResult with
        | Ok add -> add |> should equal todo
        | Error _ -> failwith "Expected Ok but got Error"
        
        let getResult = (async {
            let! res = repo.GetAllTodos()
            return res
        } |> Async.RunSynchronously)
        
        match getResult with
        | Ok todos -> todos |> should haveLength 1
                      todos.Head |> should equal todo
        | Error _ -> failwith "Expected Ok but got Error"
        
    finally
        deleteTempFile filePath

[<Fact>]
let ``GetTodoById should return the correct todo`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        let todo = { Id = Guid.NewGuid(); Text = "Test Todo"; IsCompleted = false }
        repo.AddTodo todo |> Async.RunSynchronously |> ignore

        
        let result = (async {
            let! res = repo.GetTodoById(todo.Id)
            return res
        } |> Async.RunSynchronously)
        
        match result with
        | Ok result -> result |> should equal todo
        | Error _ -> failwith "Mismatch"
    finally
        deleteTempFile filePath

[<Fact>]
let ``GetTodoById should return an error if the todo does not exist`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        
        let result = (async {
            let! res = repo.GetTodoById(Guid.NewGuid())
            return res
        } |> Async.RunSynchronously)
        
        
        
        match result with
        | Error (TodoError msg) -> msg |> should contain "Failed to load todo"
        | _ -> failwith "Expected Error but got Ok"
    finally
        deleteTempFile filePath

[<Fact>]
let ``DeleteTodoById should remove the correct todo`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        let todo = { Id = Guid.NewGuid(); Text = "Test Todo"; IsCompleted =  false }
        repo.AddTodo todo |> Async.RunSynchronously |> ignore

        
        let result = (async {
            let! res = repo.DeleteTodoById(todo.Id)
            return res
        } |> Async.RunSynchronously)
        match result with
        | Ok () -> ()
        | Error _ -> failwith "Mismatch"
        
        

        let result = (async {
            let! res = repo.GetAllTodos()
            return res
        } |> Async.RunSynchronously)
        match result with
        | Ok todos -> todos.Length |> should equal 0
        | Error _ -> failwith "Mismatch"
        
        
        
    finally
        deleteTempFile filePath

[<Fact>]
let ``DeleteTodoById should return Ok even if the todo does not exist`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        
        let result = (async {
            let! res = repo.DeleteTodoById(Guid.NewGuid())
            return res
        } |> Async.RunSynchronously)
        match result with
        | Ok () -> ()
        | Error _ -> failwith "Mismatch"
    finally
        deleteTempFile filePath

[<Fact>]
let ``UpdateTodo should update an existing todo`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        let todo = { Id = Guid.NewGuid(); Text = "Original Title"; IsCompleted = false }
        repo.AddTodo todo |> Async.RunSynchronously |> ignore

        let updatedTodo = { todo with Text = "Updated Title"; IsCompleted = true }
        let result = (async {
            let! res = repo.UpdateTodo updatedTodo
            return res
        } |> Async.RunSynchronously)
        match result with
        | Ok todo -> todo |> should equal updatedTodo
        | Error _ -> failwith "Mismatch"
        

       
        let updatedTodo = { todo with Text = "Updated Title"; IsCompleted = true }
        let result = (async {
            let! res = repo.GetTodoById(updatedTodo.Id)
            return res
        } |> Async.RunSynchronously)
        match result with
        | Ok todo -> todo |> should equal updatedTodo
        | Error _ -> failwith "Mismatch"
        
    finally
        deleteTempFile filePath

[<Fact>]
let ``UpdateTodo should return Ok even if the todo does not exist`` () =
    let filePath = createTempFile()
    try
        let repo = TodoFsRepository(filePath) :> ITodoRepository
        let todo = { Id = Guid.NewGuid(); Text = "Nonexistent Todo"; IsCompleted = false }
        
        let result = (async {
            let! res = repo.UpdateTodo todo
            return res
        } |> Async.RunSynchronously)
        match result with
        | Ok updated -> updated |> should equal todo
        | Error _ -> failwith "Mismatch"
        
    finally
        deleteTempFile filePath
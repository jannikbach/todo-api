
// open System
// open System.IO
// open Xunit
// open FsUnit
// open TodoApi.Core.Model
// open TodoApi.Infrastructure
// open TodoApi.Core.Errors
//
// let createTempFile () =
//     let tempFile = Path.GetTempFileName()
//     File.WriteAllText(tempFile, "[]") // Start with an empty todos list
//     tempFile

// let deleteTempFile (filePath: string) =
//     if File.Exists(filePath) then
//         File.Delete(filePath)
//
// [<Fact>]
// let ``GetAllTodos should return an empty list when file is empty`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let result = repo.GetAllTodos() |> Async.RunSynchronously
//         result |> should equal (Ok [])
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``AddTodo should add a todo to the list`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let todo = { Id = Guid.NewGuid(); Title = "Test Todo"; Completed = false }
//         let addResult = repo.AddTodo todo |> Async.RunSynchronously
//         addResult |> should equal (Ok todo)
//
//         let getResult = repo.GetAllTodos() |> Async.RunSynchronously
//         match getResult with
//         | Ok todos -> todos |> should haveLength 1
//                              todos.Head |> should equal todo
//         | Error _ -> failwith "Expected Ok but got Error"
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``GetTodoById should return the correct todo`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let todo = { Id = Guid.NewGuid(); Title = "Test Todo"; Completed = false }
//         repo.AddTodo todo |> Async.RunSynchronously |> ignore
//
//         let result = repo.GetTodoById(todo.Id) |> Async.RunSynchronously
//         result |> should equal (Ok todo)
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``GetTodoById should return an error if the todo does not exist`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let result = repo.GetTodoById(Guid.NewGuid()) |> Async.RunSynchronously
//         match result with
//         | Error (TodoError msg) -> msg |> should contain "Failed to load todo"
//         | _ -> failwith "Expected Error but got Ok"
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``DeleteTodoById should remove the correct todo`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let todo = { Id = Guid.NewGuid(); Title = "Test Todo"; Completed = false }
//         repo.AddTodo todo |> Async.RunSynchronously |> ignore
//
//         repo.DeleteTodoById(todo.Id) |> Async.RunSynchronously |> should equal (Ok ())
//
//         let getResult = repo.GetAllTodos() |> Async.RunSynchronously
//         getResult |> should equal (Ok [])
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``DeleteTodoById should return Ok even if the todo does not exist`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let result = repo.DeleteTodoById(Guid.NewGuid()) |> Async.RunSynchronously
//         result |> should equal (Ok ())
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``UpdateTodo should update an existing todo`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let todo = { Id = Guid.NewGuid(); Title = "Original Title"; Completed = false }
//         repo.AddTodo todo |> Async.RunSynchronously |> ignore
//
//         let updatedTodo = { todo with Title = "Updated Title"; Completed = true }
//         let updateResult = repo.UpdateTodo updatedTodo |> Async.RunSynchronously
//         updateResult |> should equal (Ok updatedTodo)
//
//         let getResult = repo.GetTodoById(updatedTodo.Id) |> Async.RunSynchronously
//         getResult |> should equal (Ok updatedTodo)
//     finally
//         deleteTempFile filePath
//
// [<Fact>]
// let ``UpdateTodo should return Ok even if the todo does not exist`` () =
//     let filePath = createTempFile()
//     try
//         let repo = TodoFsRepository(filePath) :> ITodoRepository
//         let todo = { Id = Guid.NewGuid(); Title = "Nonexistent Todo"; Completed = false }
//         let result = repo.UpdateTodo(todo) |> Async.RunSynchronously
//         result |> should equal (Ok todo)
//     finally
//         deleteTempFile filePath
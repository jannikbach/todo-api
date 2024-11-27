module TodoApi.tests


open System
open FsUnit
open NUnit.Framework.Internal
open TodoApi.Core
open Xunit
open Moq
open FsToolkit.ErrorHandling
open TodoApi.Core.Model
open TodoApi.Core.Errors
open TodoApi.Application

type ITodoTestRepository =
    inherit ITodoRepository
    abstract member GetLatestTodo: unit -> Async<Option<Todo>>


type FakeTodoRepository() =
    let mutable addedTodos: Todo list = []

    interface ITodoTestRepository with
        member _.AddTodo todo =
            addedTodos <- [todo] @ addedTodos
            async { return Ok todo }

        member this.DeleteTodoById(Id: Guid) =
            async{
                let todoExists = addedTodos |> List.exists (fun x -> x.Id = Id)
                if todoExists then
                    addedTodos <- addedTodos |> List.filter (fun x -> x.Id <> Id)
                    return Ok ()
                else
                    return Error (TodoError $"Todo with ID {Id} not found")
                }
        member this.GetAllTodos() =
            async{
                return Ok addedTodos
            }
        member this.GetTodoById(Id: Guid) =
            async {
                return Ok (addedTodos |> List.find (_.Id.Equals(Id)))
            }
        member this.UpdateTodo(todo: Todo) =
            async{
                addedTodos <- (addedTodos
                               |> List.filter (fun x -> x.Id <> todo.Id)
                               |> List.append [todo])
                return Ok todo
                }
    
        member this.GetLatestTodo() =
           async{
               match addedTodos with
               | [] -> return None
               | _ -> return Some addedTodos.Head
           }


// Test CreateTodo
[<Fact>]
let ``CreateTodo should return a valid Todo with the description text`` () =
    task {
        //Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        
        let description = "test message"
        
        
        //Act
        let result = (async{
            let! createResult = service.CreateTodo description
            return createResult
            }  |> Async.RunSynchronously)
        let result2 = (async{
            let! createResult = service.CreateTodo description
            return createResult
            }  |> Async.RunSynchronously)
        
        //Assert
        match result with
            | Ok todo -> 
                todo.Text |> should equal description
                match result2 with
                | Ok todo2 -> todo2.Id |> should not' (equal todo.Id)
                | Error _ -> failwith "Test failed: Todo2 was not added"
            | Error _ -> failwith "Test failed: Todo1 was not added"
    }
    
    
// Test GetAllTodos
[<Fact>]
let ``GetAllTodos should return all Todos created before`` () =
    task {
        //Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description = "test message"
        
        //Act
        let todo1 = {Id = Guid.NewGuid();Text = description;IsCompleted = false}
        let todo2 = {Id = Guid.NewGuid();Text = description;IsCompleted = false}
        let todo3 = {Id = Guid.NewGuid();Text = description;IsCompleted = true}
        
        repository.AddTodo todo1 |> ignore
        repository.AddTodo todo2 |> ignore
        repository.AddTodo todo3 |> ignore 
        
        let result = (async{
            let! todos = service.GetAllTodos ()
            return todos
            }  |> Async.RunSynchronously)
        
        //Assert
        match result with
            | Ok todo -> 
                todo.Length |> should equal 3
            | Error _ -> failwith "Test failed."
    }
    
// Test GetTodoById
[<Fact>]
let ``GetTodoById should return the Todos with the given Id`` () =
    task {
        //Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description = "test message"
        
        //Act
        let todo = {Id = Guid.NewGuid();Text = description;IsCompleted = false}
        repository.AddTodo todo |> ignore
        
        let resultTodo = (async{
            let! todo = service.GetTodo todo.Id
            return todo
            }  |> Async.RunSynchronously)
        
        //Assert
        match resultTodo with
            | Ok todo -> 
                todo.Id |> should equal todo.Id
            | Error _ -> failwith "Test failed."
    }
    
// Test DeleteTodo
[<Fact>]
let ``DeleteTodoById should remove the specified Todo`` () =
    task {
        // Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        let description2 = "test message 2"
        
        // Act
        let todo1 = {Id = Guid.NewGuid();Text = description1;IsCompleted = false}
        let todo2 = {Id = Guid.NewGuid();Text = description1;IsCompleted = false}
        let todo3 = {Id = Guid.NewGuid();Text = description2;IsCompleted = true}
        

        repository.AddTodo todo1 |> Async.RunSynchronously |> ignore
        repository.AddTodo todo2 |> Async.RunSynchronously |> ignore
        repository.AddTodo todo3 |> Async.RunSynchronously |> ignore
        
        
        service.RemoveTodo todo1.Id |> Async.RunSynchronously |> ignore
        
        let result = (async {
            let! todos = service.GetAllTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal 2
            todos |> List.exists (fun t -> t.Id = todo1.Id) |> should equal false
        | Error _ -> failwith "Test failed: Could not retrieve Todos after deletion."
    }
    
// Test UpdateTodo
[<Fact>]
let ``UpdateTodo should update the Todo with the matching Id`` () =
    task {
        // Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        let description2 = "test message 2"
        
        // Act
        let todo1 = {Id = Guid.NewGuid();Text = description1;IsCompleted = false}
        let todo2 = {Id = Guid.NewGuid();Text = description1;IsCompleted = false}
        let todo3 = {Id = Guid.NewGuid();Text = description2;IsCompleted = true}
        

        repository.AddTodo todo1 |> Async.RunSynchronously |> ignore
        repository.AddTodo todo2 |> Async.RunSynchronously |> ignore
        repository.AddTodo todo3 |> Async.RunSynchronously |> ignore
        
        let updateText = "updated"
        service.UpdateTodo {Id = todo1.Id; Text = updateText; IsCompleted = todo1.IsCompleted} |> Async.RunSynchronously |> ignore
        
        let result = (async {
            let! todos = service.GetAllTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal 3
            todos |> List.exists (fun t -> (t.Text = updateText && t.Id = todo1.Id)) |> should equal true
        | Error _ -> failwith "Test failed."
    }
    
    
// Test GetCompletedTodos
[<Fact>]
let ``GetCompletedTodos should only return completed Todos`` () =
    task {
        // Arrange
        
        
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        
        // Act
        let trueCount = 4
        let falseCount = 7
        for i in 1 .. trueCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = true} |> ignore
            
        for i in 1 .. falseCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = false} |> ignore
        

        let result = (async {
            let! todos = service.GetCompletedTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal trueCount
            todos |> List.exists (fun t -> (t.IsCompleted = false)) |> should equal false
        | Error _ -> failwith "Test failed."
    }
    
// Test GetUnCompletedTodos
[<Fact>]
let ``GetUnCompletedTodos should only return completed Todos`` () =
    task {
        // Arrange
        
        
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        
        // Act
        let trueCount = 5
        let falseCount = 3
        for i in 1 .. trueCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = true} |> ignore
            
        for i in 1 .. falseCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = false} |> ignore
        

        let result = (async {
            let! todos = service.GetUncompletedTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal falseCount
            todos |> List.exists (fun t -> (t.IsCompleted = true)) |> should equal false
        | Error _ -> failwith "Test failed."
    }
    
// Test UpdateText
[<Fact>]
let ``UpdateText should update only the text of the updated Todo`` () =
    task {
        // Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        let updateString = "update"
        
        // Act
        let trueCount = 5
        let falseCount = 3
        for i in 1 .. trueCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = true} |> ignore
        for i in 1 .. falseCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = false} |> ignore
        let! update = repository.GetLatestTodo()
        let updateTodo =
            match update with
            | Some todo -> todo
            | None -> failwith "No Todos found."
        
        service.UpdateText updateTodo.Id updateString  |> ignore

        let result = (async {
            let! todos = service.GetAllTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal (falseCount + trueCount)
            todos |> List.exists (fun t -> (t.Id = updateTodo.Id && t.Text = updateString)) |> should equal false
        | Error _ -> failwith "Test failed."
    }
    
    
// Test CompleteTodo
[<Fact>]
let ``CompleteTodo should set the state od the Todo to completed`` () =
    task {
        // Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        
        // Act
        let falseCount = 3
        for i in 1 .. falseCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = false} |> ignore
        let! update = repository.GetLatestTodo()
        let updateTodo =
            match update with
            | Some todo -> todo
            | None -> failwith "No Todos found."
        
        service.CompleteTodo updateTodo.Id  |> ignore

        let result = (async {
            let! todos = service.GetAllTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal (falseCount)
            todos |> List.exists (fun t -> (t.IsCompleted = true)) |> should equal false
        | Error _ -> failwith "Test failed."
    }
    
// Test UnCompleteTodo
[<Fact>]
let ``UnCompleteTodo should set the state of the Todo to uncompleted`` () =
    task {
        // Arrange
        let repository = FakeTodoRepository() :> ITodoTestRepository
        let service = TodoService(repository) :> ITodoService
        let description1 = "test message 1"
        
        // Act
        let trueCount = 3
        for i in 1 .. trueCount do
            repository.AddTodo {Id = Guid.NewGuid() ;Text = description1; IsCompleted = true} |> ignore
        let! update = repository.GetLatestTodo()
        let updateTodo =
            match update with
            | Some todo -> todo
            | None -> failwith "No Todos found."
        
        service.DeCompleteTodo updateTodo.Id  |> ignore

        let result = (async {
            let! todos = service.GetAllTodos()
            return todos
        } |> Async.RunSynchronously)
        
        // Assert
        match result with
        | Ok todos -> 
            todos.Length |> should equal (trueCount)
            todos |> List.exists (fun t -> (t.IsCompleted = false)) |> should equal false
        | Error _ -> failwith "Test failed."
    }
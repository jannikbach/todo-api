module TodoApi.Infrastructure.FileHelpers

open System.IO
open Newtonsoft.Json
open TodoApi.Core.Model

let loadTodos (filePath: string) : Todo list =
    if File.Exists(filePath) then
        let json = File.ReadAllText(filePath)
        JsonConvert.DeserializeObject<Todo list>(json)
    else
        []
let saveTodos (filePath: string) (todos: Todo list) =
    let json = JsonConvert.SerializeObject(todos, Formatting.Indented)
    File.WriteAllText(filePath, json)
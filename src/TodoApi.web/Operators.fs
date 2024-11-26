module TodoApi.Web.Operators


open System
open Giraffe
open FsToolkit.ErrorHandling
open FsToolkit.ErrorHandling
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Caching.Distributed
open Microsoft.FSharp.Data.UnitSystems.SI.UnitNames
open Newtonsoft.Json
open TodoApi.Core.Errors

type Endpoint<'T, 'U> = HttpContext -> 'T -> Async<Result<'U, AppError>>

let cache (timeout: int<second>) (endpointFunc: Endpoint<'T, 'U>): Endpoint<'T, 'U> =
    fun ctx request ->
        asyncResult {
            let cache = ctx.GetService<IDistributedCache>()
            let key = ctx.Request.Path.Value
            let data = cache.GetString(key)
            if String.IsNullOrEmpty data then
                let! result = endpointFunc ctx request
                let serializedData = JsonConvert.SerializeObject result
                let cacheOption = DistributedCacheEntryOptions(AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(int timeout))
                cache.SetString (key, serializedData, cacheOption)
                return result
            else
                return JsonConvert.DeserializeObject<'U>(data)
        }

let setStream (contentType: string) (stream : System.IO.Stream) : HttpHandler =
    fun (_ : HttpFunc) (ctx : HttpContext) ->
        task {
            ctx.SetContentType(contentType)
            return! ctx.WriteStreamAsync(false, stream, None, None)
        }


let endpointHandler (appEndpoint: Endpoint<'T, 'U>): HttpHandler =
    fun next ctx ->
        task {
            let! request =
                if typeof<'T> <> typeof<unit> then
                    ctx.BindModelAsync<'T>()
                else
                    Task.singleton Unchecked.defaultof<'T>

            let! result = appEndpoint ctx request
            match result with
            | Ok blocks -> return! json blocks next ctx
            | Error e -> return! RequestErrors.badRequest (text <| describeError e) next ctx
        }

open Giraffe.EndpointRouting
open Giraffe.OpenApi // Must come after endpoint routing

let router path (endpoint: Endpoint<'Req, 'Res>): Endpoint =
    route path (endpoint |> endpointHandler) |> addOpenApiSimple<'Req,'Res>

let routerf (path: PrintfFormat<_, _, _, _, 'Param>) (endpoint: 'Param -> Endpoint<'Req, 'Res>): Endpoint =
    routef path (endpoint >> endpointHandler) |> addOpenApiSimple<'Req,'Res>
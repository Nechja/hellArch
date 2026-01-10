namespace HellArch.HttpUtil

open System.Text.Json
open Microsoft.AspNetCore.Http

module Json =

    let options =
        JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)

module HttpResponse =

    let writeJson (ctx: HttpContext) (statusCode: int) (data: obj) =
        task {
            ctx.Response.StatusCode <- statusCode
            ctx.Response.ContentType <- "application/json"
            do! JsonSerializer.SerializeAsync(ctx.Response.Body, data, Json.options)
        }

    let writeBadRequest (ctx: HttpContext) (message: string) =
        writeJson ctx 400 {| error = "bad_request"; message = message |}

    let writeUnauthorized (ctx: HttpContext) (message: string) =
        writeJson ctx 401 {| error = "unauthorized"; message = message |}

    let writeForbidden (ctx: HttpContext) (message: string) =
        writeJson ctx 403 {| error = "forbidden"; message = message |}

    let writeNotFound (ctx: HttpContext) (message: string) =
        writeJson ctx 404 {| error = "not_found"; message = message |}

    let writeConflict (ctx: HttpContext) (message: string) =
        writeJson ctx 409 {| error = "conflict"; message = message |}

    let writeServerError (ctx: HttpContext) (message: string) =
        writeJson ctx 500 {| error = "internal_error"; message = message |}

namespace User.Api

open System.Text.Json
open Microsoft.AspNetCore.Http
open User.Domain

module HttpUtils =

    let jsonOptions =
        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)
        options.WriteIndented <- true
        options

    let writeJson (ctx: HttpContext) (statusCode: int) (data: obj) =
        task {
            ctx.Response.StatusCode <- statusCode
            ctx.Response.ContentType <- "application/json"
            do! ctx.Response.WriteAsync(JsonSerializer.Serialize(data, jsonOptions))
        }

    let handleError (ctx: HttpContext) (error: UserError) =
        match error with
        | UserError.NotFound id -> writeJson ctx 404 {| error = "not_found"; message = $"User {id} not found" |}
        | UserError.ValidationError msg -> writeJson ctx 400 {| error = "validation_error"; message = msg |}
        | UserError.DatabaseError _ -> writeJson ctx 500 {| error = "internal_error"; message = "Database error" |}
        | UserError.Conflict msg -> writeJson ctx 409 {| error = "conflict"; message = msg |}

    let writeBadRequest (ctx: HttpContext) (message: string) =
        writeJson ctx 400 {| error = "bad_request"; message = message |}

    let writeUnauthorized (ctx: HttpContext) (message: string) =
        writeJson ctx 401 {| error = "unauthorized"; message = message |}

    let writeNotFound (ctx: HttpContext) (message: string) =
        writeJson ctx 404 {| error = "not_found"; message = message |}

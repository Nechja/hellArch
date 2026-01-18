namespace User.Api

open Microsoft.AspNetCore.Http
open User.Domain
open HellArch.HttpUtil

module HttpUtils =

    let jsonOptions = Json.options

    let writeJson = HttpResponse.writeJson

    let handleError (ctx: HttpContext) (error: UserError) =
        match error with
        | UserError.NotFound id -> HttpResponse.writeNotFound ctx $"User {id} not found"
        | UserError.ValidationError msg -> HttpResponse.writeBadRequest ctx msg
        | UserError.DatabaseError _ -> HttpResponse.writeServerError ctx "Database error"
        | UserError.Conflict msg -> HttpResponse.writeConflict ctx msg

    let writeBadRequest = HttpResponse.writeBadRequest

    let writeUnauthorized = HttpResponse.writeUnauthorized

    let writeForbidden = HttpResponse.writeForbidden

    let writeNotFound = HttpResponse.writeNotFound

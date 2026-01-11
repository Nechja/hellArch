namespace User.Api

open Microsoft.AspNetCore.Http
open User.Infrastructure
open User.Api.HttpUtils

module HealthHandlers =

    let health (dbConfig: Database.DatabaseConfig) (ctx: HttpContext) =
        task {
            let! dbOk = Database.checkConnection dbConfig |> Async.StartAsTask
            let status = if dbOk then "healthy" else "unhealthy"
            let statusCode = if dbOk then 200 else 503
            do! writeJson ctx statusCode {| status = status; database = dbOk |}
        }

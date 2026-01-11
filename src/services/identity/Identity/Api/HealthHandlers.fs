namespace Identity.Api

open Microsoft.AspNetCore.Http
open Identity.Api.HttpUtils

module HealthHandlers =

    let handleHealth (ctx: HttpContext) =
        task {
            do! writeJson ctx 200 {| status = "healthy" |}
        }

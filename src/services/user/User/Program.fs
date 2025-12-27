open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open User.Api
open User.Infrastructure

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    let app = builder.Build()

    let dbConfig: Database.DatabaseConfig =
        { ConnectionString =
            Environment.GetEnvironmentVariable("USER_DATABASE_URL")
            |> Option.ofObj
            |> Option.defaultValue "Host=localhost;Port=5433;Database=user_db;Username=hellarch;Password=hellarch" }

    // Health endpoint
    app.MapGet("/health", Func<HttpContext, _>(Handlers.health)) |> ignore

    // Profile endpoints
    app.MapGet(
        "/user/{userId:guid}",
        Func<Guid, HttpContext, _>(fun userId ctx -> Handlers.getProfile dbConfig userId ctx)
    )
    |> ignore

    app.MapPost("/user", Func<HttpContext, _>(fun ctx -> Handlers.createProfile dbConfig ctx))
    |> ignore

    app.MapPut(
        "/user/{userId:guid}",
        Func<Guid, HttpContext, _>(fun userId ctx -> Handlers.updateProfile dbConfig userId ctx)
    )
    |> ignore

    // Preferences endpoints
    app.MapGet(
        "/user/{userId:guid}/preferences",
        Func<Guid, HttpContext, _>(fun userId ctx -> Handlers.getPreferences dbConfig userId ctx)
    )
    |> ignore

    app.MapPut(
        "/user/{userId:guid}/preferences",
        Func<Guid, HttpContext, _>(fun userId ctx -> Handlers.updatePreferences dbConfig userId ctx)
    )
    |> ignore

    printfn "User service starting on http://localhost:5002"
    app.Run("http://localhost:5002")
    0

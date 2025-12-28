open System
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Npgsql
open OpenTelemetry.Resources
open OpenTelemetry.Trace
open User.Api
open User.Infrastructure

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    let logLevel =
        match Environment.GetEnvironmentVariable("LOG_LEVEL") |> Option.ofObj |> Option.map (fun s -> s.ToLowerInvariant()) with
        | Some "info" -> LogLevel.Information
        | Some "warn" | Some "warning" -> LogLevel.Warning
        | Some "error" -> LogLevel.Error
        | _ -> LogLevel.Debug // Default to debug for dev

    builder.Logging.ClearProviders() |> ignore
    builder.Logging.SetMinimumLevel(logLevel) |> ignore
    builder.Logging.AddJsonConsole(fun options ->
        options.IncludeScopes <- true
        options.TimestampFormat <- "yyyy-MM-ddTHH:mm:ssZ"
        options.JsonWriterOptions <- JsonWriterOptions(Indented = false)
    ) |> ignore

    let otelEndpoint =
        Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
        |> Option.ofObj
        |> Option.defaultValue "http://localhost:4318"

    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(fun resource -> resource.AddService("user") |> ignore)
        .WithTracing(fun tracing ->
            tracing
                .AddSource("hellarch/user/events")
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddOtlpExporter(fun options ->
                    options.Endpoint <- Uri(otelEndpoint)
                ) |> ignore
        ) |> ignore

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

    // RabbitMQ consumer configuration
    let eventsConfig: Events.EventsConfig =
        { RabbitMQUrl =
            Environment.GetEnvironmentVariable("RABBITMQ_URL")
            |> Option.ofObj
            |> Option.defaultValue "amqp://guest:guest@localhost:5672/"
          QueueName = "user-service" }

    // Start consumer as background task
    let cts = new CancellationTokenSource()
    let consumerTask = Events.startConsumer eventsConfig dbConfig cts.Token

    // Handle graceful shutdown
    app.Lifetime.ApplicationStopping.Register(fun () ->
        printfn "Shutting down event consumer..."
        cts.Cancel()
        try
            consumerTask.Wait(TimeSpan.FromSeconds(5.0)) |> ignore
        with
        | :? AggregateException -> ()
        | :? TaskCanceledException -> ()
    ) |> ignore

    printfn "User service starting on http://localhost:5002"
    app.Run("http://localhost:5002")
    0

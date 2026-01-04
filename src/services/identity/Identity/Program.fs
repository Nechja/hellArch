open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open OpenTelemetry.Resources
open OpenTelemetry.Trace
open Identity.Api

let getEnv name defaultValue =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> defaultValue
    | value -> value

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    // Configuration
    let connectionString = getEnv "IDENTITY_DATABASE_URL" "Host=localhost;Port=5437;Database=identity_db;Username=hellarch;Password=hellarch"
    let clientBaseUrl = getEnv "IDENTITY_CLIENT_URL" "http://localhost:5000/identity"
    let httpPort = getEnv "HTTP_PORT" "5010" |> int
    let serviceName = getEnv "OTEL_SERVICE_NAME" "identity"
    let otlpEndpoint = getEnv "OTEL_EXPORTER_OTLP_ENDPOINT" ""

    let config: Handlers.Config = {
        ConnectionString = connectionString
        ClientBaseUrl = clientBaseUrl
        AccessTokenTtl = TimeSpan.FromHours(1.0)
        RefreshTokenTtl = TimeSpan.FromDays(30.0)
        AuthCodeTtl = TimeSpan.FromMinutes(5.0)
    }

    // OpenTelemetry
    if not (String.IsNullOrEmpty(otlpEndpoint)) then
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(fun r -> r.AddService(serviceName) |> ignore)
            .WithTracing(fun tracing ->
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(fun opts -> opts.Endpoint <- Uri(otlpEndpoint))
                |> ignore)
        |> ignore

    // CORS for the identity client
    builder.Services.AddCors(fun options ->
        options.AddDefaultPolicy(fun policy ->
            policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
            |> ignore))
    |> ignore

    let app = builder.Build()

    app.UseCors() |> ignore

    // Routes
    app.MapGet("/identity/authorize", Func<HttpContext, _>(Handlers.handleAuthorize config)) |> ignore
    app.MapPost("/identity/login", Func<HttpContext, _>(Handlers.handleLogin config)) |> ignore
    app.MapPost("/identity/register", Func<HttpContext, _>(Handlers.handleRegister config)) |> ignore
    app.MapPost("/identity/token", Func<HttpContext, _>(Handlers.handleToken config)) |> ignore
    app.MapGet("/identity/userinfo", Func<HttpContext, _>(Handlers.handleUserInfo config)) |> ignore
    app.MapGet("/identity/health", Func<HttpContext, _>(Handlers.handleHealth)) |> ignore

    app.Urls.Add($"http://0.0.0.0:{httpPort}")

    printfn $"Identity service starting on port {httpPort}"
    app.Run()

    0

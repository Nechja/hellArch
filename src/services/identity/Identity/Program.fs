open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open OpenTelemetry.Resources
open OpenTelemetry.Trace
open Identity.Api
open Identity.Infrastructure

let getEnv name defaultValue =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> defaultValue
    | value -> value

let getEnvOpt name =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> None
    | value -> Some value

let seedClientIfConfigured (connectionString: string) =
    let clientId = getEnvOpt "IDENTITY_SEED_CLIENT_ID"
    let clientSecret = getEnvOpt "IDENTITY_SEED_CLIENT_SECRET"
    let redirectUris = getEnv "IDENTITY_SEED_REDIRECT_URIS" "http://localhost:5000/api/auth/hellarch/callback"

    match clientId, clientSecret with
    | Some id, Some secret ->
        // Check if client already exists
        match Database.getClient connectionString id |> Async.RunSynchronously with
        | Ok (Some _) ->
            printfn $"[Seed] Client '{id}' already exists, skipping"
        | Ok None ->
            // Hash the secret and create client
            let secretHash = PasswordHasher.hash secret
            let redirectUriList = redirectUris.Split(',') |> Array.map (fun s -> s.Trim()) |> Array.toList

            let client: HellArch.DataTypes.Identity.OAuthClient = {
                Id = id
                SecretHash = secretHash
                Name = "HellArch Auth Service"
                RedirectUris = redirectUriList
                IsActive = true
                CreatedAt = DateTime.UtcNow
            }

            match Database.createClient connectionString client |> Async.RunSynchronously with
            | Ok () -> printfn $"[Seed] Created OAuth client '{id}' with {redirectUriList.Length} redirect URI(s)"
            | Error err -> printfn $"[Seed] Failed to create client: {err}"
        | Error err ->
            printfn $"[Seed] Error checking for existing client: {err}"
    | _ -> ()

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

    // Seed client if configured
    seedClientIfConfigured connectionString

    printfn $"Identity service starting on port {httpPort}"
    app.Run()

    0

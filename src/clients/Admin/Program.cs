using System.Text.Json;
using Admin.Components;
using Admin.Middleware;
using Admin.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Logging
var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL")?.ToLowerInvariant() switch
{
    "info" => LogLevel.Information,
    "warn" or "warning" => LogLevel.Warning,
    "error" => LogLevel.Error,
    _ => LogLevel.Debug
};

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(logLevel);
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
    options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
});

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// HTTP clients for backend services
var authUrl = Environment.GetEnvironmentVariable("AUTH_URL") ?? "http://localhost:5001";
var userUrl = Environment.GetEnvironmentVariable("USER_URL") ?? "http://localhost:5002";

builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(authUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(userUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Redis direct connection for key browsing
var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var options = ConfigurationOptions.Parse(redisUrl);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

// Services
builder.Services.AddScoped<AuthAdminFacade>();
builder.Services.AddScoped<RedisBrowserAdapter>();

// OpenTelemetry
var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://localhost:4318";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("admin"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otelEndpoint);
            });
    });

var app = builder.Build();

app.UseStaticFiles();
app.UseAntiforgery();

// Auth middleware - validates session before serving admin pages
app.UseMiddleware<AdminAuthMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Run($"http://0.0.0.0:{port}");

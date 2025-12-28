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

// HTTP client for Gateway (routes to backend services via YARP)
var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL") ?? "http://localhost:5000";

builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(gatewayUrl + "/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(gatewayUrl + "/api/");
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

// RabbitMQ Management API
var rabbitmqUrl = Environment.GetEnvironmentVariable("RABBITMQ_MANAGEMENT_URL") ?? "http://localhost:15672";
builder.Services.AddHttpClient("RabbitMQ", client =>
{
    client.BaseAddress = new Uri(rabbitmqUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Services
builder.Services.AddScoped<AuthAdminFacade>();
builder.Services.AddScoped<RedisBrowserAdapter>();
builder.Services.AddScoped<RabbitMqAdapter>();

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

// Handle path base from X-Forwarded-Prefix header (set by Gateway)
app.Use((context, next) =>
{
    var forwardedPrefix = context.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault();
    if (!string.IsNullOrEmpty(forwardedPrefix))
    {
        context.Request.PathBase = forwardedPrefix;
    }
    return next();
});

// Serve static files from wwwroot (css, js, images)
app.UseStaticFiles();

app.UseAntiforgery();

// Auth middleware - validates session before serving admin pages
app.UseMiddleware<AdminAuthMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Run($"http://0.0.0.0:{port}");

using System.Text.Json;
using Admin.Components;
using Admin.Middleware;
using Admin.Services;
using HellArch.Http;
using MudBlazor.Services;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add resilient HTTP handling to all HttpClients
builder.Services.AddResilientHttpClients();

// Configure W3C TraceContext propagation for distributed tracing
Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([
    new TraceContextPropagator(),
    new BaggagePropagator()
]));

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
    .AddInteractiveServerComponents(options =>
    {
        // Enable detailed errors for debugging
        options.DetailedErrors = true;
    });

builder.Services.AddMudServices();

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

builder.Services.AddHttpClient("EventsService", client =>
{
    client.BaseAddress = new Uri(gatewayUrl + "/api/events/");
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

// Health aggregator service (direct internal access)
var healthUrl = Environment.GetEnvironmentVariable("HEALTH_SERVICE_URL") ?? "http://health:5004";
builder.Services.AddHttpClient("HealthService", client =>
{
    client.BaseAddress = new Uri(healthUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Testing service (direct internal access)
var testingUrl = Environment.GetEnvironmentVariable("TESTING_SERVICE_URL") ?? "http://testing:5007";
builder.Services.AddHttpClient("TestingService", client =>
{
    client.BaseAddress = new Uri(testingUrl + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IAuthAdminFacade, AuthAdminFacade>();
builder.Services.AddScoped<IEventsAdminFacade, EventsAdminFacade>();
builder.Services.AddScoped<ITestsAdminFacade, TestsAdminFacade>();
builder.Services.AddScoped<RedisBrowserAdapter>();
builder.Services.AddScoped<IRabbitMqAdapter, RabbitMqAdapter>();
builder.Services.AddScoped<IHealthAdapter, HealthAdapter>();
builder.Services.AddSingleton<ISessionValidator, SessionValidator>();

// Realtime SignalR hub connection (singleton for shared connection, lazy connect)
builder.Services.AddSingleton<IHubConnectionFactory, HubConnectionFactory>();
builder.Services.AddSingleton<RealtimeHubService>();
builder.Services.AddSingleton<IToastService, ToastService>();
builder.Services.AddSingleton<LiveUpdateService>();
builder.Services.AddHttpContextAccessor();

var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? "http://localhost:4318";

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "admin";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
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

app.UseStaticFiles();

app.UseAntiforgery();

// Health check endpoint (before auth middleware so it's not protected)
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Auth middleware - validates session before serving admin pages
app.UseMiddleware<AdminAuthMiddleware>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

var port = Environment.GetEnvironmentVariable("PORT") ?? "5003";
app.Run($"http://0.0.0.0:{port}");

using System.Text.Json;
using System.Threading.RateLimiting;
using Gateway.Middleware;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var logLevelMappings = new Dictionary<string, LogLevel>(StringComparer.OrdinalIgnoreCase)
{
    ["debug"] = LogLevel.Debug,
    ["info"] = LogLevel.Information,
    ["warn"] = LogLevel.Warning,
    ["warning"] = LogLevel.Warning,
    ["error"] = LogLevel.Error
};

var logLevelValue = Environment.GetEnvironmentVariable("LOG_LEVEL") ?? "debug";
var logLevel = logLevelMappings.GetValueOrDefault(logLevelValue, LogLevel.Debug);

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(logLevel);
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ssZ";
    options.JsonWriterOptions = new JsonWriterOptions { Indented = false };
});

var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? builder.Configuration["OpenTelemetry:Endpoint"]
    ?? "http://localhost:4318";

if (!Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var otelUri))
{
    throw new InvalidOperationException($"Invalid OTEL_EXPORTER_OTLP_ENDPOINT: {otelEndpoint}");
}

// Configure W3C TraceContext propagation for distributed tracing
Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator([
    new TraceContextPropagator(),
    new BaggagePropagator()
]));

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "gateway";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options => options.Endpoint = otelUri));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// gRPC reflection for development tooling (grpcurl, Postman)
builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClients", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5010"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddHttpClient();

var rateLimitConfig = builder.Configuration.GetSection("RateLimiting");
var permitLimit = rateLimitConfig.GetValue("PermitLimit", 100);
var windowMinutes = rateLimitConfig.GetValue("WindowMinutes", 1);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes)
            }));
});

var app = builder.Build();

// Security headers should be first to apply to all responses
app.UseMiddleware<SecurityHeadersMiddleware>();

// Whitelist of allowed error pages to prevent path traversal
var allowedErrorPages = new Dictionary<int, string>
{
    [403] = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "errors", "403.html"),
    [404] = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "errors", "404.html")
};

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var acceptHeader = context.HttpContext.Request.Headers.Accept.ToString();

    // Only serve HTML error pages for browser requests
    if (acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase))
    {
        if (allowedErrorPages.TryGetValue(response.StatusCode, out var errorPage) && File.Exists(errorPage))
        {
            response.ContentType = "text/html";
            await response.SendFileAsync(errorPage);
        }
    }
});

app.UseCors("AllowClients");
app.UseRateLimiter();

app.UseMiddleware<AuthCookieToJwtMiddleware>();
app.UseMiddleware<AuthRedirectMiddleware>();

app.UseHealthChecks("/health");

// Serve static files for SPAs (login, app, etc.)
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true // For WASM files
});

app.UseRouting();

// WebSocket support for SignalR (Blazor Server circuits and realtime hubs)
app.UseWebSockets();

// SPA fallback routes - serve index.html for client-side routing
app.MapFallbackToFile("/login/{**path}", "login/index.html");
app.MapFallbackToFile("/app/{**path}", "app/index.html");

app.MapReverseProxy();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.Run();

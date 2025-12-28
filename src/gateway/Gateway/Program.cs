using System.Text.Json;
using System.Threading.RateLimiting;
using Gateway.Middleware;
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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("gateway"))
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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

app.UseCors("AllowClients");
app.UseRateLimiter();

app.UseMiddleware<AuthCookieToJwtMiddleware>();

app.UseHealthChecks("/health");

app.MapReverseProxy();

if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.Run();

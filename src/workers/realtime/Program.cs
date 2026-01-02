using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Realtime.Hubs;
using Realtime.Services;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables with prefix support
builder.Configuration.AddEnvironmentVariables();

// Logging
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

// OpenTelemetry
var otelEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
    ?? builder.Configuration["OpenTelemetry:Endpoint"]
    ?? "http://localhost:4318";

var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "realtime-worker";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSource("realtime-worker")
        .AddOtlpExporter(options => options.Endpoint = new Uri(otelEndpoint)));

// JWT Authentication for SignalR (RSA public key validation)
var jwtPublicKeyPath = builder.Configuration["Jwt:PublicKeyPath"];
if (string.IsNullOrEmpty(jwtPublicKeyPath))
{
    jwtPublicKeyPath = Environment.GetEnvironmentVariable("JWT_PUBLIC_KEY_PATH");
}

if (string.IsNullOrEmpty(jwtPublicKeyPath))
{
    throw new InvalidOperationException("Jwt:PublicKeyPath configuration or JWT_PUBLIC_KEY_PATH environment variable is required");
}

var publicKeyText = File.ReadAllText(jwtPublicKeyPath);
var rsa = System.Security.Cryptography.RSA.Create();
rsa.ImportFromPem(publicKeyText);
var rsaSecurityKey = new RsaSecurityKey(rsa);

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "hellarch-auth";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = rsaSecurityKey,
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR sends token in query string for WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// CORS for SignalR clients
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5000", "http://localhost:5010"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalR", policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Health checks
builder.Services.AddHealthChecks();

// RabbitMQ consumer
builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.UseCors("SignalR");
app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/health");

app.MapHub<EventsHub>("/hubs/events");

app.Run();

using System.Security.Claims;
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
#pragma warning disable CA2000 // RSA instance is owned by RsaSecurityKey and must remain undisposed
var rsa = System.Security.Cryptography.RSA.Create();
#pragma warning restore CA2000
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
#pragma warning disable CA5404 // Internal service does not require audience validation
            ValidateAudience = false,
#pragma warning restore CA5404
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        // SignalR sends token in query string for WebSocket connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = async context =>
            {
                var path = context.HttpContext.Request.Path;
                if (!path.StartsWithSegments("/hubs"))
                {
                    return;
                }

                // Try JWT access_token first
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken;
                    return;
                }

                // Fallback: session_token validation via auth service
                var sessionToken = context.Request.Query["session_token"];
                if (string.IsNullOrEmpty(sessionToken))
                {
                    return;
                }

                var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("SessionAuth");

                try
                {
                    using var client = httpClientFactory.CreateClient("AuthService");
                    using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/validate");
                    request.Headers.Add("X-Session-Token", sessionToken.ToString());

                    using var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogDebug("Session validation failed: {StatusCode}", response.StatusCode);
                        return;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(content);

                    if (!doc.RootElement.TryGetProperty("user_id", out var userIdElement))
                    {
                        logger.LogDebug("No user_id in session validation response");
                        return;
                    }

                    var userId = userIdElement.GetString();
                    if (string.IsNullOrEmpty(userId))
                    {
                        return;
                    }

                    // Build claims from session validation response
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, userId),
                        new("sub", userId)
                    };

                    if (doc.RootElement.TryGetProperty("username", out var usernameElement))
                    {
                        var username = usernameElement.GetString();
                        if (!string.IsNullOrEmpty(username))
                        {
                            claims.Add(new Claim(ClaimTypes.Name, username));
                        }
                    }

                    // Check for admin permission
                    if (doc.RootElement.TryGetProperty("permissions", out var permissionsElement))
                    {
                        if (permissionsElement.TryGetProperty("admin.panel", out var adminPerm) &&
                            adminPerm.GetBoolean())
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "admin"));
                        }
                    }

                    var identity = new ClaimsIdentity(claims, "SessionToken");
                    context.Principal = new ClaimsPrincipal(identity);
                    context.Success();

                    logger.LogDebug("Session authenticated for user {UserId}", userId);
                }
                catch (HttpRequestException ex)
                {
                    logger.LogWarning(ex, "HTTP error validating session");
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to parse session validation response");
                }
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

// HTTP client for session validation (fallback auth)
var authServiceUrl = Environment.GetEnvironmentVariable("AUTH_SERVICE_URL")
    ?? builder.Configuration["Auth:ServiceUrl"]
    ?? "http://hellarch-auth:5001";

builder.Services.AddHttpClient("AuthService", client =>
{
    client.BaseAddress = new Uri(authServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// RabbitMQ consumer
builder.Services.AddHostedService<EventConsumer>();

var app = builder.Build();

app.UseCors("SignalR");
app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/health");

app.MapHub<EventsHub>("/hubs/events");

app.Run();

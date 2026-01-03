using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace Gateway.Middleware;

internal sealed record SessionInfo(string UserId, string? Username, string? Permissions, bool IsActive);

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core middleware pipeline")]
internal sealed partial class AuthRedirectMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    IWebHostEnvironment environment,
    ILogger<AuthRedirectMiddleware> logger)
{
    private readonly string _cookieName = configuration.GetValue("Auth:CookieName", "hellarch_session")!;
    private readonly string _loginPath = configuration.GetValue("Auth:LoginPath", "/login")!;
    private readonly string _defaultRedirect = configuration.GetValue("Auth:DefaultRedirect", "/admin")!;
    private readonly string _validateEndpoint = configuration.GetValue("Auth:ValidateEndpoint", "http://hellarch-auth:5001/auth/validate")!;
    private readonly string[] _protectedPaths = configuration.GetSection("Auth:ProtectedPaths").Get<string[]>() ?? ["/admin", "/app"];
    private readonly string[] _bypassPaths = ["/login", "/api/auth", "/health", "/hubs", "/_framework", "/_content", "/css", "/js", "/lib", "/favicon"];
    private readonly Dictionary<string, string> _permissionRoutes = configuration.GetSection("Auth:PermissionRoutes").Get<Dictionary<string, string>>() ?? [];

    [LoggerMessage(Level = LogLevel.Debug, Message = "Path {Path} requires authentication, redirecting to login")]
    private partial void LogRedirectingToLogin(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Session validated for path {Path}")]
    private partial void LogSessionValidated(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Session validation failed: {Reason}")]
    private partial void LogValidationFailed(string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Bypassing auth for path {Path}")]
    private partial void LogBypassingAuth(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Permission denied for user {UserId} on path {Path}, required: {Permission}")]
    private partial void LogPermissionDenied(string userId, string path, string permission);

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path.Value ?? "/";

        // Handle root path redirect
        if (path == "/")
        {
            var rootSession = await ValidateSession(context).ConfigureAwait(false);
            var redirectTo = rootSession is not null ? _defaultRedirect : _loginPath;
            context.Response.Redirect(redirectTo);
            return;
        }

        // Bypass paths that don't need user context
        if (ShouldBypass(path))
        {
            LogBypassingAuth(path);
            await next(context).ConfigureAwait(false);
            return;
        }

        // Try to validate session and add user context
        var session = await ValidateSession(context).ConfigureAwait(false);

        if (session is not null)
        {
            // Check if user is active before any permission checks (defense in depth)
            if (!session.IsActive)
            {
                LogValidationFailed("user is inactive");
                context.Response.Redirect(_loginPath);
                return;
            }

            // Check if route requires specific permission
            var requiredPermission = GetRequiredPermission(path);
            if (requiredPermission is not null && !HasPermission(session.Permissions, requiredPermission))
            {
                LogPermissionDenied(session.UserId, path, requiredPermission);
                await WritePermissionDenied(context, requiredPermission).ConfigureAwait(false);
                return;
            }

            context.Request.Headers["X-User-ID"] = session.UserId;
            if (session.Username is not null)
            {
                context.Request.Headers["X-Username"] = session.Username;
            }
            if (session.Permissions is not null)
            {
                context.Request.Headers["X-Permissions"] = session.Permissions;
            }
            LogSessionValidated(path);
        }
        else if (IsProtectedPath(path))
        {
            // Protected paths require auth - redirect to login
            LogRedirectingToLogin(path);
            var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect($"{_loginPath}?return_url={returnUrl}");
            return;
        }

        await next(context).ConfigureAwait(false);
    }

    private bool ShouldBypass(string path)
    {
        foreach (var bypassPath in _bypassPaths)
        {
            if (path.StartsWith(bypassPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Also bypass static file extensions
        if (path.Contains('.') && !path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var extension = Path.GetExtension(path);
            if (extension is ".css" or ".js" or ".wasm" or ".dll" or ".json" or ".png" or ".jpg" or ".svg" or ".ico" or ".woff" or ".woff2")
            {
                return true;
            }
        }

        return false;
    }

    private bool IsProtectedPath(string path)
    {
        foreach (var protectedPath in _protectedPaths)
        {
            if (path.StartsWith(protectedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private string? GetRequiredPermission(string path)
    {
        // Find the most specific matching route (longest prefix match)
        string? bestMatch = null;
        string? bestPermission = null;

        foreach (var (route, permission) in _permissionRoutes)
        {
            if (path.StartsWith(route, StringComparison.OrdinalIgnoreCase) &&
                (bestMatch is null || route.Length > bestMatch.Length))
            {
                bestMatch = route;
                bestPermission = permission;
            }
        }

        return bestPermission;
    }

    private bool HasPermission(string? permissionsJson, string requiredPermission)
    {
        if (string.IsNullOrEmpty(permissionsJson))
        {
            logger.LogDebug("No permissions JSON provided");
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(permissionsJson);
            if (doc.RootElement.TryGetProperty(requiredPermission, out var value))
            {
                return value.ValueKind == JsonValueKind.True;
            }
            return false;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse permissions JSON");
            return false;
        }
    }

    private async Task WritePermissionDenied(HttpContext context, string requiredPermission)
    {
        logger.LogDebug("Writing 403 response for missing permission {Permission}", requiredPermission);
        context.Response.StatusCode = 403;

        // Serve HTML for browser requests, JSON for API requests
        var acceptHeader = context.Request.Headers.Accept.ToString();
        if (acceptHeader.Contains("text/html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.ContentType = "text/html";
            var errorPagePath = Path.Combine(environment.ContentRootPath, "wwwroot", "errors", "403.html");
            if (File.Exists(errorPagePath))
            {
                await context.Response.SendFileAsync(errorPagePath).ConfigureAwait(false);
                return;
            }
        }

        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            $$"""{"error": "forbidden", "message": "Permission denied", "required": "{{requiredPermission}}"}"""
        ).ConfigureAwait(false);
    }

    private async Task<SessionInfo?> ValidateSession(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(_cookieName, out var sessionToken) || string.IsNullOrEmpty(sessionToken))
        {
            return null;
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, _validateEndpoint);
            request.Headers.Add("X-Session-Token", sessionToken);

            using var response = await client.SendAsync(request).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogValidationFailed($"Status {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(content);

            if (!doc.RootElement.TryGetProperty("user_id", out var userIdElement))
            {
                LogValidationFailed("No user_id in response");
                return null;
            }

            var userId = userIdElement.GetString();
            if (string.IsNullOrEmpty(userId))
            {
                LogValidationFailed("Empty user_id in response");
                return null;
            }

            // Extract username if present
            string? username = null;
            if (doc.RootElement.TryGetProperty("username", out var usernameElement))
            {
                username = usernameElement.GetString();
            }

            // Extract permissions if present
            string? permissions = null;
            if (doc.RootElement.TryGetProperty("permissions", out var permissionsElement))
            {
                permissions = permissionsElement.GetRawText();
            }

            // Extract is_active (default to true for backwards compatibility)
            var isActive = true;
            if (doc.RootElement.TryGetProperty("is_active", out var isActiveElement))
            {
                isActive = isActiveElement.GetBoolean();
            }

            return new SessionInfo(userId, username, permissions, isActive);
        }
        catch (HttpRequestException ex)
        {
            LogValidationFailed(ex.Message);
            return null;
        }
        catch (JsonException ex)
        {
            LogValidationFailed($"JSON parse error: {ex.Message}");
            return null;
        }
    }
}

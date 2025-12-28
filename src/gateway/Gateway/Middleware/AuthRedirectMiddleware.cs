using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Primitives;

namespace Gateway.Middleware;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core middleware pipeline")]
internal sealed partial class AuthRedirectMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<AuthRedirectMiddleware> logger)
{
    private readonly string _cookieName = configuration.GetValue("Auth:CookieName", "hellarch_session")!;
    private readonly string _loginPath = configuration.GetValue("Auth:LoginPath", "/login")!;
    private readonly string _defaultRedirect = configuration.GetValue("Auth:DefaultRedirect", "/admin")!;
    private readonly string _validateEndpoint = configuration.GetValue("Auth:ValidateEndpoint", "http://hellarch-auth:5001/auth/validate")!;
    private readonly string[] _protectedPaths = configuration.GetSection("Auth:ProtectedPaths").Get<string[]>() ?? ["/admin", "/app"];
    private readonly string[] _bypassPaths = ["/login", "/api/auth", "/health", "/_framework", "/_content", "/css", "/js", "/lib", "/favicon"];

    [LoggerMessage(Level = LogLevel.Debug, Message = "Path {Path} requires authentication, redirecting to login")]
    private partial void LogRedirectingToLogin(string path);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Session validated for path {Path}")]
    private partial void LogSessionValidated(string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Session validation failed: {Reason}")]
    private partial void LogValidationFailed(string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Bypassing auth for path {Path}")]
    private partial void LogBypassingAuth(string path);

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var path = context.Request.Path.Value ?? "/";

        // Handle root path redirect
        if (path == "/")
        {
            var isAuthenticated = await ValidateSession(context).ConfigureAwait(false);
            var redirectTo = isAuthenticated ? _defaultRedirect : _loginPath;
            context.Response.Redirect(redirectTo);
            return;
        }

        // Bypass paths that don't require auth
        if (ShouldBypass(path))
        {
            LogBypassingAuth(path);
            await next(context).ConfigureAwait(false);
            return;
        }

        // Check if path is protected
        if (IsProtectedPath(path))
        {
            var isAuthenticated = await ValidateSession(context).ConfigureAwait(false);

            if (!isAuthenticated)
            {
                LogRedirectingToLogin(path);
                var returnUrl = Uri.EscapeDataString(context.Request.Path + context.Request.QueryString);
                context.Response.Redirect($"{_loginPath}?return_url={returnUrl}");
                return;
            }

            LogSessionValidated(path);
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

    private async Task<bool> ValidateSession(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(_cookieName, out var sessionToken) || string.IsNullOrEmpty(sessionToken))
        {
            return false;
        }

        try
        {
            using var client = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, _validateEndpoint);
            request.Headers.Add("X-Session-Token", sessionToken);

            using var response = await client.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            LogValidationFailed($"Status {response.StatusCode}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            LogValidationFailed(ex.Message);
            return false;
        }
    }
}

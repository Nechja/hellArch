using System.Text.Json;
using System.Text.Json.Serialization;

namespace Admin.Middleware;

public sealed class AdminAuthMiddleware(
    RequestDelegate next,
    IHttpClientFactory httpClientFactory,
    ILogger<AdminAuthMiddleware> logger)
{
    private const string SessionCookieName = "hellarch_session";
    private const string AuthServiceName = "AuthService";
    private const string RequiredPermission = "admin.access";

    public async Task InvokeAsync(HttpContext context)
    {
        // Allow static files and health checks without auth
        if (context.Request.Path.StartsWithSegments("/_framework") ||
            context.Request.Path.StartsWithSegments("/_blazor") ||
            context.Request.Path.StartsWithSegments("/css") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken) ||
            string.IsNullOrEmpty(sessionToken))
        {
            logger.LogDebug("No session cookie found, redirecting to login");
            RedirectToLogin(context);
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient(AuthServiceName);
            client.DefaultRequestHeaders.Add("X-Session-Token", sessionToken);

            var response = await client.PostAsync("auth/validate", null);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Session validation failed with status {Status}", response.StatusCode);
                RedirectToLogin(context);
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<ValidateResponse>();

            if (result is null || !result.Valid)
            {
                logger.LogDebug("Session is not valid");
                RedirectToLogin(context);
                return;
            }

            // Check for admin.access permission
            if (!HasPermission(result.Permissions, RequiredPermission))
            {
                logger.LogWarning("User {UserId} lacks {Permission} permission", result.UserId, RequiredPermission);
                await ShowAccessDenied(context, result.Username ?? result.Email ?? "User");
                return;
            }

            // Store user info in HttpContext for downstream use
            context.Items["UserId"] = result.UserId;
            context.Items["Email"] = result.Email;
            context.Items["Username"] = result.Username;
            context.Items["Permissions"] = result.Permissions;
            context.Items["SessionToken"] = sessionToken;
            context.Items["IsAuthenticated"] = true;

            logger.LogDebug("Authenticated admin user {UserId}", result.UserId);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP error validating session");
            RedirectToLogin(context);
            return;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Session validation timed out");
            RedirectToLogin(context);
            return;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse session validation response");
            RedirectToLogin(context);
            return;
        }

        await next(context);
    }

    private void RedirectToLogin(HttpContext context)
    {
        // Build return URL - supports both direct access and proxy access
        var scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault()
            ?? context.Request.Scheme;
        var host = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
            ?? context.Request.Host.ToString();

        // For direct access (localhost:5003), redirect to Gateway for auth
        var gatewayHost = host.Contains(":5003") ? "localhost:5000" : host;
        var returnHost = host.Contains(":5003") ? "localhost:5003" : $"{host}/admin";

        var returnUrl = Uri.EscapeDataString($"{scheme}://{returnHost}{context.Request.Path}");
        var redirectUrl = $"{scheme}://{gatewayHost}/api/auth/discord/login?redirect={returnUrl}";

        logger.LogDebug("Redirecting to login: {Url}", redirectUrl);
        context.Response.Redirect(redirectUrl);
    }

    private bool HasPermission(Dictionary<string, bool>? permissions, string permission)
    {
        var hasPermission = permissions is not null &&
                            permissions.TryGetValue(permission, out var granted) &&
                            granted;

        logger.LogDebug("Permission check: {Permission} = {HasPermission}", permission, hasPermission);
        return hasPermission;
    }

    private async Task ShowAccessDenied(HttpContext context, string username)
    {
        logger.LogInformation("Access denied for user {Username} - missing {Permission}", username, RequiredPermission);

        context.Response.StatusCode = 403;
        context.Response.ContentType = "text/html; charset=utf-8";

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Access Denied - HellArch Admin</title>
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body {
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                        background: #0d1117;
                        color: #f0f6fc;
                        min-height: 100vh;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                    }
                    .container {
                        text-align: center;
                        padding: 2rem;
                    }
                    h1 {
                        font-size: 4rem;
                        color: #f85149;
                        margin-bottom: 1rem;
                    }
                    p {
                        color: #8b949e;
                        margin-bottom: 0.5rem;
                    }
                    .username {
                        color: #58a6ff;
                        font-weight: 500;
                    }
                    .permission {
                        font-family: monospace;
                        background: #21262d;
                        padding: 0.25rem 0.5rem;
                        border-radius: 4px;
                        color: #f85149;
                    }
                    a {
                        display: inline-block;
                        margin-top: 1.5rem;
                        padding: 0.5rem 1rem;
                        background: #21262d;
                        color: #58a6ff;
                        text-decoration: none;
                        border-radius: 6px;
                        border: 1px solid #30363d;
                    }
                    a:hover {
                        background: #30363d;
                    }
                </style>
            </head>
            <body>
                <div class="container">
                    <h1>403</h1>
                    <p>Access Denied</p>
                    <p>Hello <span class="username">{{System.Web.HttpUtility.HtmlEncode(username)}}</span>, you don't have permission to access the Admin panel.</p>
                    <p>Required permission: <span class="permission">{{RequiredPermission}}</span></p>
                    <a href="/">Return Home</a>
                </div>
            </body>
            </html>
            """;

        await context.Response.WriteAsync(html);
    }

    private sealed record ValidateResponse(
        bool Valid,
        [property: JsonPropertyName("user_id")] string? UserId,
        string? Email,
        string? Username,
        Dictionary<string, bool>? Permissions);
}

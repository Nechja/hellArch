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

        // Check if request came through gateway (has X-User-ID header)
        var gatewayUserId = context.Request.Headers["X-User-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(gatewayUserId))
        {
            // Trust gateway - it already validated session and permissions
            context.Items["UserId"] = gatewayUserId;
            context.Items["Permissions"] = context.Request.Headers["X-Permissions"].FirstOrDefault();
            context.Items["IsAuthenticated"] = true;

            logger.LogDebug("Trusted gateway auth for user {UserId}", gatewayUserId);
            await next(context);
            return;
        }

        // Direct access - validate session ourselves
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

            // For direct access, check admin.panel permission
            if (!HasPermission(result.Permissions, "admin.panel"))
            {
                logger.LogWarning("User {UserId} lacks admin.panel permission (direct access)", result.UserId);
                context.Response.StatusCode = 403;
                await context.Response.WriteAsync("Forbidden: admin.panel permission required");
                return;
            }

            // Store user info in HttpContext for downstream use
            context.Items["UserId"] = result.UserId;
            context.Items["Email"] = result.Email;
            context.Items["Username"] = result.Username;
            context.Items["Permissions"] = result.Permissions;
            context.Items["SessionToken"] = sessionToken;
            context.Items["IsAuthenticated"] = true;

            logger.LogDebug("Authenticated admin user {UserId} (direct access)", result.UserId);
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

    private sealed record ValidateResponse(
        bool Valid,
        [property: JsonPropertyName("user_id")] string? UserId,
        string? Email,
        string? Username,
        Dictionary<string, bool>? Permissions);
}

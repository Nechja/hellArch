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

        // Check for session cookie
        if (!context.Request.Cookies.TryGetValue(SessionCookieName, out var sessionToken) ||
            string.IsNullOrEmpty(sessionToken))
        {
            logger.LogDebug("No session cookie found, redirecting to login");
            RedirectToLogin(context);
            return;
        }

        // Validate session against auth service
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

            // Store user info in HttpContext for downstream use
            context.Items["UserId"] = result.UserId;
            context.Items["Email"] = result.Email;
            context.Items["Username"] = result.Username;
            context.Items["SessionToken"] = sessionToken;
            context.Items["IsAuthenticated"] = true;

            logger.LogDebug("Authenticated user {UserId}", result.UserId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate session");
            RedirectToLogin(context);
            return;
        }

        await next(context);
    }

    private static void RedirectToLogin(HttpContext context)
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
        context.Response.Redirect($"{scheme}://{gatewayHost}/api/auth/discord/login?redirect={returnUrl}");
    }

    private sealed record ValidateResponse(
        bool Valid,
        string? UserId,
        string? Email,
        string? Username);
}

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

            var response = await client.PostAsync("/auth/validate", null);

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
        // Redirect to auth service login with return URL
        var returnUrl = Uri.EscapeDataString($"http://localhost:5003{context.Request.Path}");
        context.Response.Redirect($"http://localhost:5000/api/auth/discord/login?redirect={returnUrl}");
    }

    private sealed record ValidateResponse(
        bool Valid,
        string? UserId,
        string? Email,
        string? Username);
}

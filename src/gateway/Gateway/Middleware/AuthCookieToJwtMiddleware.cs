using System.Diagnostics.CodeAnalysis;

namespace Gateway.Middleware;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core middleware pipeline")]
internal sealed partial class AuthCookieToJwtMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<AuthCookieToJwtMiddleware> logger)
{
    private readonly string _cookieName = configuration.GetValue("Auth:CookieName", "hellarch_session")!;
    private const int MaxTokenLength = 512;

    [LoggerMessage(Level = LogLevel.Warning, Message = "Session token exceeds maximum length, ignoring")]
    private partial void LogTokenTooLong();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Forwarded session token to upstream service")]
    private partial void LogTokenForwarded();

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Cookies.TryGetValue(_cookieName, out var sessionToken))
        {
            if (sessionToken.Length > MaxTokenLength)
            {
                LogTokenTooLong();
            }
            else
            {
                context.Request.Headers.Append("X-Session-Token", sessionToken);
                LogTokenForwarded();
            }
        }

        await next(context).ConfigureAwait(false);
    }
}

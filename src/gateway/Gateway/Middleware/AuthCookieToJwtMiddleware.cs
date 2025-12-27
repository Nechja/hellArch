using System.Diagnostics.CodeAnalysis;

namespace Gateway.Middleware;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core middleware pipeline")]
internal sealed class AuthCookieToJwtMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string _cookieName = configuration.GetValue("Auth:CookieName", "hellarch_session")!;

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Request.Cookies.TryGetValue(_cookieName, out var sessionToken))
        {
            context.Request.Headers.Append("X-Session-Token", sessionToken);
        }

        await next(context).ConfigureAwait(false);
    }
}

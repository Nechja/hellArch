using System.Diagnostics.CodeAnalysis;

namespace Gateway.Middleware;

[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core middleware pipeline")]
internal sealed partial class SecurityHeadersMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ILogger<SecurityHeadersMiddleware> logger)
{
    private readonly bool _useHsts = configuration.GetValue("Security:UseHsts", true);
    private readonly int _hstsMaxAge = configuration.GetValue("Security:HstsMaxAgeSeconds", 31536000); // 1 year
    private readonly string _frameOptions = configuration.GetValue("Security:FrameOptions", "DENY")!;
    private readonly string _referrerPolicy = configuration.GetValue("Security:ReferrerPolicy", "strict-origin-when-cross-origin")!;
    private readonly string? _contentSecurityPolicy = configuration.GetValue<string>("Security:ContentSecurityPolicy");

    [LoggerMessage(Level = LogLevel.Debug, Message = "Applied security headers to response")]
    private partial void LogHeadersApplied();

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // Prevent MIME-sniffing attacks
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking
            headers["X-Frame-Options"] = _frameOptions;

            // Control referrer information leakage
            headers["Referrer-Policy"] = _referrerPolicy;

            // Disable deprecated XSS filter (can cause issues, CSP is better)
            headers["X-XSS-Protection"] = "0";

            // Prevent cross-origin information leakage
            headers["Cross-Origin-Opener-Policy"] = "same-origin";

            // HSTS - force HTTPS (only set over HTTPS to avoid issues)
            if (_useHsts && context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = $"max-age={_hstsMaxAge}; includeSubDomains";
            }

            // Content Security Policy (if configured)
            if (!string.IsNullOrEmpty(_contentSecurityPolicy))
            {
                headers["Content-Security-Policy"] = _contentSecurityPolicy;
            }

            // Permissions Policy - disable unnecessary browser features
            headers["Permissions-Policy"] = "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

            LogHeadersApplied();
            return Task.CompletedTask;
        });

        await next(context).ConfigureAwait(false);
    }
}

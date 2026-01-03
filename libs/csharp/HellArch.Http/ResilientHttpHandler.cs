using Microsoft.Extensions.Logging;

namespace HellArch.Http;

public sealed partial class ResilientHttpHandler(ILogger<ResilientHttpHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method;
        var uri = request.RequestUri;

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(method, uri, (int)response.StatusCode);
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
            LogHttpRequestException(method, uri, ex);
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimeout(method, uri, ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Method} {Uri} returned {StatusCode}")]
    private partial void LogHttpFailure(HttpMethod method, Uri? uri, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Method} {Uri} failed")]
    private partial void LogHttpRequestException(HttpMethod method, Uri? uri, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Method} {Uri} timed out")]
    private partial void LogTimeout(HttpMethod method, Uri? uri, Exception ex);
}

using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HellArch.Http;

public static class HttpClientExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> GetJsonSafe<T>(
        this HttpClient client,
        string requestUri,
        ILogger logger,
        CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            return await client.GetFromJsonAsync<T>(requestUri, JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP request failed for {Uri}", requestUri);
            return default;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "Request timed out for {Uri}", requestUri);
            return default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize response from {Uri}", requestUri);
            return default;
        }
    }

    public static async Task<bool> PostJsonSafe<T>(
        this HttpClient client,
        string requestUri,
        T content,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.PostAsJsonAsync(requestUri, content, JsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP POST failed for {Uri}", requestUri);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "POST request timed out for {Uri}", requestUri);
            return false;
        }
    }

    public static async Task<TResponse?> PostJsonSafe<TRequest, TResponse>(
        this HttpClient client,
        string requestUri,
        TRequest content,
        ILogger logger,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        try
        {
            var response = await client.PostAsJsonAsync(requestUri, content, JsonOptions, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("POST to {Uri} returned {StatusCode}", requestUri, response.StatusCode);
                return default;
            }
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP POST failed for {Uri}", requestUri);
            return default;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "POST request timed out for {Uri}", requestUri);
            return default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize POST response from {Uri}", requestUri);
            return default;
        }
    }

    public static async Task<TResponse?> PostJsonSafe<TResponse>(
        this HttpClient client,
        string requestUri,
        ILogger logger,
        CancellationToken cancellationToken = default) where TResponse : class
    {
        try
        {
            var response = await client.PostAsync(requestUri, null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("POST to {Uri} returned {StatusCode}", requestUri, response.StatusCode);
                return default;
            }
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP POST failed for {Uri}", requestUri);
            return default;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "POST request timed out for {Uri}", requestUri);
            return default;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize POST response from {Uri}", requestUri);
            return default;
        }
    }

    public static async Task<bool> PutJsonSafe<T>(
        this HttpClient client,
        string requestUri,
        T content,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.PutAsJsonAsync(requestUri, content, JsonOptions, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP PUT failed for {Uri}", requestUri);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "PUT request timed out for {Uri}", requestUri);
            return false;
        }
    }

    public static async Task<bool> DeleteSafe(
        this HttpClient client,
        string requestUri,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.DeleteAsync(requestUri, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "HTTP DELETE failed for {Uri}", requestUri);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "DELETE request timed out for {Uri}", requestUri);
            return false;
        }
    }
}

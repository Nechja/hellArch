using Microsoft.AspNetCore.SignalR.Client;

namespace Admin.Services;

/// <summary>
/// Default implementation of IHubConnectionFactory that creates SignalR connections
/// with session token authentication.
/// </summary>
public sealed class HubConnectionFactory(IConfiguration configuration) : IHubConnectionFactory
{
    public HubConnection Create(string sessionToken)
    {
        var gatewayUrl = Environment.GetEnvironmentVariable("GATEWAY_URL")
            ?? configuration.GetValue<string>("Gateway:Url")
            ?? "http://localhost:5000";

        var hubUrl = $"{gatewayUrl}/hubs/events";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(null);
                options.Headers["X-Session-Token"] = sessionToken;
                options.Url = new Uri($"{hubUrl}?session_token={Uri.EscapeDataString(sessionToken)}");
            })
            .WithAutomaticReconnect([TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)])
            .Build();

        return connection;
    }
}

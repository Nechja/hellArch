using System.Net;
using Gateway.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Gateway.Tests;

public class AuthRedirectMiddlewareTests : IDisposable
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<IWebHostEnvironment> _environment;
    private readonly Mock<ILogger<AuthRedirectMiddleware>> _logger;
    private readonly IConfiguration _configuration;
    private readonly List<IDisposable> _disposables = [];
    private bool _disposed;

    public AuthRedirectMiddlewareTests()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _environment = new Mock<IWebHostEnvironment>();
        _logger = new Mock<ILogger<AuthRedirectMiddleware>>();

        _environment.Setup(e => e.ContentRootPath).Returns("/app");

        var configData = new Dictionary<string, string?>
        {
            ["Auth:CookieName"] = "test_session",
            ["Auth:LoginPath"] = "/login",
            ["Auth:DefaultRedirect"] = "/admin",
            ["Auth:ValidateEndpoint"] = "http://auth:5001/auth/validate",
            ["Auth:ProtectedPaths:0"] = "/admin",
            ["Auth:ProtectedPaths:1"] = "/app"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Theory]
    [InlineData("/app/login", true)]      // Bypass - specific login page
    [InlineData("/app/register", true)]   // Bypass - specific register page
    [InlineData("/api/auth", true)]       // Bypass - auth endpoints
    [InlineData("/health", true)]         // Bypass - health checks
    [InlineData("/app", false)]           // NOT bypass - protected app root
    [InlineData("/app/dashboard", false)] // NOT bypass - protected app page
    [InlineData("/app/settings", false)]  // NOT bypass - protected app page
    [InlineData("/admin", false)]         // NOT bypass - protected admin area
    public async Task BypassPathsOnlyAllowsSpecificPaths(string path, bool shouldBypass)
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext(path);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        if (shouldBypass)
        {
            Assert.True(nextCalled, $"Path {path} should bypass authentication");
            Assert.NotEqual(302, context.Response.StatusCode);
        }
        else
        {
            // Protected paths without session should redirect to login
            Assert.Equal(302, context.Response.StatusCode);
            Assert.Contains("/login", context.Response.Headers.Location.ToString());
        }
    }

    [Fact]
    public async Task ForbiddenResponseDoesNotLeakRequiredPermission()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["Auth:CookieName"] = "test_session",
            ["Auth:LoginPath"] = "/login",
            ["Auth:ValidateEndpoint"] = "http://auth:5001/auth/validate",
            ["Auth:ProtectedPaths:0"] = "/admin",
            ["Auth:PermissionRoutes:/admin/secrets"] = "admin:secrets"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        // Mock successful session validation but without required permission
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"user_id": "user-123", "permissions": {}}""")
        };
        var mockHandler = CreateMockHttpHandler(response);
        using var client = new HttpClient(mockHandler.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var middleware = new AuthRedirectMiddleware(
            next, config, _httpClientFactory.Object, _environment.Object, _logger.Object);

        var context = CreateHttpContext("/admin/secrets");
        context.Request.Cookies = new MockCookieCollection(new Dictionary<string, string>
        {
            ["test_session"] = "valid-token"
        });
        context.Request.Headers.Accept = "application/json";

        var responseBody = new MemoryStream();
        _disposables.Add(responseBody);
        context.Response.Body = responseBody;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(403, context.Response.StatusCode);
        Assert.False(nextCalled, "Middleware should not call next for forbidden");

        responseBody.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(responseBody, leaveOpen: true);
        var body = await reader.ReadToEndAsync();

        // Response should NOT contain the required permission name
        Assert.DoesNotContain("admin:secrets", body);
        Assert.DoesNotContain("required", body);

        // But should contain generic forbidden message
        Assert.Contains("forbidden", body);
        Assert.Contains("Permission denied", body);
    }

    [Fact]
    public async Task ValidateSessionHandlesTimeout()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        // Mock handler that delays beyond timeout
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage _, CancellationToken ct) =>
            {
                // This will throw OperationCanceledException when CTS times out
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        using var client = new HttpClient(mockHandler.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext("/admin/dashboard");
        context.Request.Cookies = new MockCookieCollection(new Dictionary<string, string>
        {
            ["test_session"] = "valid-token"
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should redirect to login due to timeout (session validation failed)
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Contains("/login", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task ProtectedPathWithoutSessionRedirectsToLogin()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext("/app/dashboard");

        // No session cookie

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(302, context.Response.StatusCode);
        var location = context.Response.Headers.Location.ToString();
        Assert.Contains("/login", location);
        Assert.Contains("return_url", location);
    }

    [Fact]
    public async Task InactiveUserRedirectsToLogin()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"user_id": "user-123", "is_active": false}""")
        };
        var mockHandler = CreateMockHttpHandler(response);
        using var client = new HttpClient(mockHandler.Object);
        _httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        var middleware = CreateMiddleware(next);
        var context = CreateHttpContext("/app/dashboard");
        context.Request.Cookies = new MockCookieCollection(new Dictionary<string, string>
        {
            ["test_session"] = "valid-token"
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert - inactive user should be redirected to login
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Contains("/login", context.Response.Headers.Location.ToString());
    }

    private AuthRedirectMiddleware CreateMiddleware(RequestDelegate next)
    {
        return new AuthRedirectMiddleware(
            next, _configuration, _httpClientFactory.Object, _environment.Object, _logger.Object);
    }

    private static DefaultHttpContext CreateHttpContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Cookies = new MockCookieCollection(new Dictionary<string, string>());
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Mock<HttpMessageHandler> CreateMockHttpHandler(HttpResponseMessage response)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return mockHandler;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }

        _disposed = true;
    }
}

internal sealed class MockCookieCollection(Dictionary<string, string> cookies) : IRequestCookieCollection
{
    private readonly Dictionary<string, string> _cookies = cookies;

    public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;
    public int Count => _cookies.Count;
    public ICollection<string> Keys => _cookies.Keys;
    public bool ContainsKey(string key) => _cookies.ContainsKey(key);

#pragma warning disable CS8767 // Nullability of reference types doesn't match because test mock
    public bool TryGetValue(string key, out string? value)
    {
        var result = _cookies.TryGetValue(key, out var v);
        value = v;
        return result;
    }
#pragma warning restore CS8767

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

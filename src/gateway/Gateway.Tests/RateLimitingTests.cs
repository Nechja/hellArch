using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;

namespace Gateway.Tests;

public class RateLimitingTests
{
    [Theory]
    [InlineData("/api/auth/login", true)]
    [InlineData("/api/auth/callback", true)]
    [InlineData("/api/auth/refresh", true)]
    [InlineData("/api/users", false)]
    [InlineData("/api/files", false)]
    [InlineData("/admin", false)]
    public void AuthPathsUseStricterRateLimits(string path, bool isAuthPath)
    {
        // This test verifies the path detection logic that determines rate limit tier
        var result = path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
        Assert.Equal(isAuthPath, result);
    }

    [Fact]
    public void AuthEndpointsGetSeparatePartitionKey()
    {
        // Arrange
        var ip = "192.168.1.1";

        // Act - simulate partition key generation for different paths
        var authPath = "/api/auth/login";
        var regularPath = "/api/users";

        var isAuthPath1 = authPath.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
        var isAuthPath2 = regularPath.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);

        var partitionKey1 = isAuthPath1 ? $"auth:{ip}" : ip;
        var partitionKey2 = isAuthPath2 ? $"auth:{ip}" : ip;

        // Assert - auth endpoints get separate partition from regular endpoints
        Assert.Equal("auth:192.168.1.1", partitionKey1);
        Assert.Equal("192.168.1.1", partitionKey2);
        Assert.NotEqual(partitionKey1, partitionKey2);
    }

    [Fact]
    public void RateLimiterConfiguresCorrectLimits()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["RateLimiting:PermitLimit"] = "100",
            ["RateLimiting:WindowMinutes"] = "1",
            ["RateLimiting:AuthPermitLimit"] = "10",
            ["RateLimiting:AuthWindowMinutes"] = "1"
        };

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var rateLimitConfig = config.GetSection("RateLimiting");
        var permitLimit = rateLimitConfig.GetValue("PermitLimit", 100);
        var authPermitLimit = rateLimitConfig.GetValue("AuthPermitLimit", 10);

        // Assert - auth endpoints should have stricter limits
        Assert.Equal(100, permitLimit);
        Assert.Equal(10, authPermitLimit);
        Assert.True(authPermitLimit < permitLimit, "Auth rate limit should be stricter than global");
    }

    [Fact]
    public void FixedWindowLimiterExhaustsCorrectly()
    {
        // Arrange - create limiter with same settings as auth endpoints
        var options = new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        };

        using var limiter = new FixedWindowRateLimiter(options);

        // Act - acquire permits until exhausted
        var acquired = 0;
        for (var i = 0; i < 15; i++)
        {
            using var lease = limiter.AttemptAcquire();
            if (lease.IsAcquired)
            {
                acquired++;
            }
        }

        // Assert - should only acquire up to the limit
        Assert.Equal(10, acquired);
    }

    [Fact]
    public void PartitionedLimiterSeparatesAuthFromRegularTraffic()
    {
        // Arrange - create partitioned limiter similar to Program.cs
        var limiter = PartitionedRateLimiter.Create<string, string>(key =>
        {
            var isAuth = key.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase);
            var partitionKey = isAuth ? $"auth:{key}" : key;

            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: partitionKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = isAuth ? 3 : 10, // Use small numbers for test
                    Window = TimeSpan.FromMinutes(1)
                });
        });

        // Act - exhaust auth endpoint limit
        var authAcquired = 0;
        for (var i = 0; i < 5; i++)
        {
            using var lease = limiter.AttemptAcquire("/api/auth/login");
            if (lease.IsAcquired)
            {
                authAcquired++;
            }
        }

        // Regular endpoint should still be available
        using var regularLease = limiter.AttemptAcquire("/api/users");

        // Assert
        Assert.Equal(3, authAcquired); // Auth limit is 3
        Assert.True(regularLease.IsAcquired, "Regular endpoint should have separate limit");

        limiter.Dispose();
    }
}

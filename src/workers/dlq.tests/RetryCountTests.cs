using DlqWorker.Services;

namespace DlqWorker.Tests;

/// <summary>
/// Tests for retry count extraction from RabbitMQ headers.
/// RabbitMQ can serialize header values as different types (int, long, byte[])
/// depending on protocol version and client library - we must handle all.
/// </summary>
public class RetryCountTests
{
    [Fact]
    public void ReturnsZeroForNullHeaders()
    {
        var result = DlqConsumer.GetRetryCount(null);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ReturnsZeroForEmptyHeaders()
    {
        var headers = new Dictionary<string, object?>();

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ReturnsZeroWhenHeaderMissing()
    {
        var headers = new Dictionary<string, object?>
        {
            ["other-header"] = 42
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ExtractsIntValue()
    {
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = 3
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(3, result);
    }

    [Fact]
    public void ExtractsLongValue()
    {
        // RabbitMQ sometimes sends numbers as long
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = 5L
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(5, result);
    }

    [Fact]
    public void ExtractsByteArrayValue()
    {
        // AMQP protocol can encode small integers as byte arrays
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = BitConverter.GetBytes(7)
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(7, result);
    }

    [Fact]
    public void ReturnsZeroForUnexpectedType()
    {
        // If someone puts a string or other unexpected type, default to 0
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = "three"
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(0, result);
    }

    [Fact]
    public void ReturnsZeroForNullValue()
    {
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = null
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(0, result);
    }

    [Fact]
    public void HandlesZeroRetryCount()
    {
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = 0
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(0, result);
    }

    [Fact]
    public void HandlesLargeRetryCount()
    {
        // Edge case: very large retry count (shouldn't happen in practice)
        var headers = new Dictionary<string, object?>
        {
            ["x-dlq-retry-count"] = 1000
        };

        var result = DlqConsumer.GetRetryCount(headers);

        Assert.Equal(1000, result);
    }
}

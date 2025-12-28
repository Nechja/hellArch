using StackExchange.Redis;

namespace Admin.Services;

public sealed class RedisBrowserAdapter(
    IConnectionMultiplexer redis,
    ILogger<RedisBrowserAdapter> logger)
{
    public async Task<IReadOnlyList<RedisKeyInfo>> ScanKeys(string pattern = "*", int count = 100)
    {
        try
        {
            var server = redis.GetServer(redis.GetEndPoints().First());
            var db = redis.GetDatabase();
            var keys = new List<RedisKeyInfo>();

            await foreach (var key in server.KeysAsync(pattern: pattern, pageSize: count))
            {
                var keyType = await db.KeyTypeAsync(key);
                var ttl = await db.KeyTimeToLiveAsync(key);

                keys.Add(new RedisKeyInfo(
                    key.ToString(),
                    keyType.ToString(),
                    ttl));

                if (keys.Count >= count)
                {
                    break;
                }
            }

            return keys;
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis error scanning keys with pattern {Pattern}", pattern);
            return [];
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation scanning Redis keys with pattern {Pattern}", pattern);
            return [];
        }
    }

    public async Task<RedisKeyDetail?> GetKeyDetail(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            var keyType = await db.KeyTypeAsync(key);
            var ttl = await db.KeyTimeToLiveAsync(key);

            string? value = keyType switch
            {
                RedisType.String => await db.StringGetAsync(key),
                RedisType.Hash => FormatHash(await db.HashGetAllAsync(key)),
                RedisType.List => FormatList(await db.ListRangeAsync(key, 0, 100)),
                RedisType.Set => FormatSet(await db.SetMembersAsync(key)),
                RedisType.SortedSet => FormatSortedSet(await db.SortedSetRangeByRankWithScoresAsync(key, 0, 100)),
                _ => $"[{keyType}]"
            };

            return new RedisKeyDetail(key, keyType.ToString(), value, ttl);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis error getting key detail for {Key}", key);
            return null;
        }
    }

    public async Task<bool> DeleteKey(string key)
    {
        try
        {
            var db = redis.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis error deleting key {Key}", key);
            return false;
        }
    }

    public RedisConnectionInfo GetConnectionInfo()
    {
        try
        {
            var endpoints = redis.GetEndPoints();
            var isConnected = redis.IsConnected;
            var configuration = redis.Configuration;

            return new RedisConnectionInfo(
                isConnected,
                endpoints.Select(e => e.ToString() ?? "unknown").ToList(),
                configuration);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Redis error getting connection info");
            return new RedisConnectionInfo(false, [], "unknown");
        }
    }

    private static string FormatHash(HashEntry[] entries) =>
        string.Join("\n", entries.Select(e => $"{e.Name}: {e.Value}"));

    private static string FormatList(RedisValue[] values) =>
        string.Join("\n", values.Select((v, i) => $"[{i}] {v}"));

    private static string FormatSet(RedisValue[] members) =>
        string.Join("\n", members);

    private static string FormatSortedSet(SortedSetEntry[] entries) =>
        string.Join("\n", entries.Select(e => $"{e.Element} (score: {e.Score})"));
}

public sealed record RedisKeyInfo(
    string Key,
    string Type,
    TimeSpan? Ttl);

public sealed record RedisKeyDetail(
    string Key,
    string Type,
    string? Value,
    TimeSpan? Ttl);

public sealed record RedisConnectionInfo(
    bool IsConnected,
    IReadOnlyList<string> Endpoints,
    string Configuration);

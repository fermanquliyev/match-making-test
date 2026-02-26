using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Redis;

public sealed class RedisPendingRequestStore(
    IConnectionMultiplexer redis,
    ILogger<RedisPendingRequestStore> logger) : IPendingRequestStore
{
    private static readonly TimeSpan PendingTtl = TimeSpan.FromMinutes(5);
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> TrySetPendingAsync(string userId, CancellationToken cancellationToken)
    {
        var key = RedisKeyNames.PendingRequest(userId);
        try
        {
            var set = await _db.StringSetAsync(key, "1", PendingTtl, when: When.NotExists);
            return set;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set pending for userId {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsPendingAsync(string userId, CancellationToken cancellationToken)
    {
        var key = RedisKeyNames.PendingRequest(userId);
        return await _db.KeyExistsAsync(key);
    }

    public async Task ClearPendingAsync(string userId, CancellationToken cancellationToken)
    {
        var key = RedisKeyNames.PendingRequest(userId);
        await _db.KeyDeleteAsync(key);
    }
}

using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Redis;

public sealed class RedisRateLimitStore(
    IConnectionMultiplexer redis,
    ILogger<RedisRateLimitStore> logger) : IRateLimitStore
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<bool> TryAcquireAsync(string userId, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyNames.RateLimit(userId);
        try
        {
            var tran = _db.CreateTransaction();
            var exists = tran.StringGetAsync(key);
            _ = tran.StringSetAsync(key, "1", window, when: When.NotExists);
            var committed = await tran.ExecuteAsync();
            if (!committed)
            {
                logger.LogDebug("Rate limit: key already set for userId {UserId}", userId);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rate limit check failed for userId {UserId}", userId);
            throw;
        }
    }
}

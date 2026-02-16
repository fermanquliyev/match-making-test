using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Redis;

public sealed class RedisMatchQueueStore(
    IConnectionMultiplexer redis,
    ILogger<RedisMatchQueueStore> logger) : IMatchQueueStore
{
    private readonly IDatabase _db = redis.GetDatabase();
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    public async Task EnqueueAsync(string userId, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyNames.Queue;
        await _db.ListRightPushAsync(key, userId);
    }

    public async Task<IReadOnlyList<string>?> TryDequeueBatchAsync(int count, CancellationToken cancellationToken = default)
    {
        var lockKey = RedisKeyNames.QueueLock;
        var lockValue = Guid.NewGuid().ToString("N");
        var acquired = await _db.StringSetAsync(lockKey, lockValue, LockTimeout, when: When.NotExists);
        if (!acquired)
            return null;

        try
        {
            var key = RedisKeyNames.Queue;
            var length = await _db.ListLengthAsync(key);
            if (length < count)
                return null;

            var userIds = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var value = await _db.ListLeftPopAsync(key);
                if (value.IsNullOrEmpty) break;
                userIds.Add(value!);
            }

            if (userIds.Count != count)
            {
                foreach (var id in userIds.AsEnumerable().Reverse())
                    await _db.ListLeftPushAsync(key, id);
                return null;
            }

            return userIds;
        }
        finally
        {
            var current = await _db.StringGetAsync(lockKey);
            if (current == lockValue)
                await _db.KeyDeleteAsync(lockKey);
        }
    }

    public async Task RequeueBatchAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken = default)
    {
        var key = RedisKeyNames.Queue;
        for (var i = userIds.Count - 1; i >= 0; i--)
            await _db.ListLeftPushAsync(key, userIds[i]);
    }
}

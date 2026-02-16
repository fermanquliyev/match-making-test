namespace MatchMaking.Infrastructure.Redis;

public static class RedisKeyNames
{
    public const string KeyPrefix = "matchmaking:";

    public static string RateLimit(string userId) => $"{KeyPrefix}ratelimit:{userId}";
    public static string PendingRequest(string userId) => $"{KeyPrefix}pending:{userId}";
    public static string Match(string matchId) => $"{KeyPrefix}match:{matchId}";
    public static string UserMatch(string userId) => $"{KeyPrefix}usermatch:{userId}";
    public static string Queue => $"{KeyPrefix}queue";
    public static string QueueLock => $"{KeyPrefix}queue:lock";
}

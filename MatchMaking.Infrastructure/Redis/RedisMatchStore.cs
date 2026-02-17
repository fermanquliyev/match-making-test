using System.Text.Json;
using MatchMaking.Application.Interfaces;
using MatchMaking.Domain;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure.Redis;

public sealed class RedisMatchStore(
    IConnectionMultiplexer redis,
    ILogger<RedisMatchStore> logger) : IMatchStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task SaveMatchAsync(Match match, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var key = RedisKeyNames.Match(match.MatchId);
        var json = JsonSerializer.Serialize(new { match.MatchId, match.UserIds, match.CreatedAtUtc }, JsonOptions);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task<Match?> GetMatchByMatchIdAsync(string matchId, CancellationToken cancellationToken)
    {
        var key = RedisKeyNames.Match(matchId);
        var json = await _db.StringGetAsync(key);
        if (json.IsNullOrEmpty) return null;
        var dto = JsonSerializer.Deserialize<MatchDto>(json!, JsonOptions);
        return dto is null ? null : new Match(dto.MatchId, dto.UserIds, dto.CreatedAtUtc);
    }

    public async Task SetUserMatchAsync(string userId, string matchId, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var key = RedisKeyNames.UserMatch(userId);
        await _db.StringSetAsync(key, matchId, ttl);
    }

    public async Task<Match?> GetMatchByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        var userKey = RedisKeyNames.UserMatch(userId);
        var matchId = await _db.StringGetAsync(userKey);
        if (matchId.IsNullOrEmpty) return null;
        return await GetMatchByMatchIdAsync(matchId!, cancellationToken);
    }

    private sealed record MatchDto(string MatchId, string[] UserIds, DateTime CreatedAtUtc);
}

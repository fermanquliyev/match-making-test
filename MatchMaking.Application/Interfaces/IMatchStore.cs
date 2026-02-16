using MatchMaking.Domain;

namespace MatchMaking.Application.Interfaces;

public interface IMatchStore
{
    Task SaveMatchAsync(Match match, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<Match?> GetMatchByMatchIdAsync(string matchId, CancellationToken cancellationToken = default);
    Task SetUserMatchAsync(string userId, string matchId, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task<Match?> GetMatchByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

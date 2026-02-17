using MatchMaking.Domain;

namespace MatchMaking.Application.Interfaces;

public interface IMatchStore
{
    Task SaveMatchAsync(Match match, TimeSpan ttl, CancellationToken cancellationToken);
    Task<Match?> GetMatchByMatchIdAsync(string matchId, CancellationToken cancellationToken);
    Task SetUserMatchAsync(string userId, string matchId, TimeSpan ttl, CancellationToken cancellationToken);
    Task<Match?> GetMatchByUserIdAsync(string userId, CancellationToken cancellationToken);
}

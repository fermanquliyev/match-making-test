using MatchMaking.Domain;

namespace MatchMaking.Application.Interfaces;

public interface IMatchQueryService
{
    Task<Match?> GetMatchByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

using MatchMaking.Application.Interfaces;

namespace MatchMaking.Application.Implementations;

public sealed class MatchQueryService(IMatchStore matchStore) : IMatchQueryService
{
    public async Task<Domain.Match?> GetMatchByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await matchStore.GetMatchByUserIdAsync(userId, cancellationToken);
    }
}

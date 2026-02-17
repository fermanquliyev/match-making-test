using MatchMaking.Application.Interfaces;
using MatchMaking.Domain;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Application.Implementations;

public sealed class MatchCompletionHandler(
    IMatchStore matchStore,
    IPendingRequestStore pendingRequestStore,
    ILogger<MatchCompletionHandler> logger) : IMatchCompletionHandler
{
    private static readonly TimeSpan MatchTtl = TimeSpan.FromMinutes(30);

    public async Task HandleMatchCompleteAsync(string matchId, IReadOnlyList<string> userIds, DateTime createdAtUtc, CancellationToken cancellationToken)
    {
        var existing = await matchStore.GetMatchByMatchIdAsync(matchId, cancellationToken);
        if (existing is not null)
        {
            logger.LogDebug("Match {MatchId} already stored, skipping (idempotent)", matchId);
            return;
        }

        var match = new Match(matchId, userIds, createdAtUtc);
        await matchStore.SaveMatchAsync(match, MatchTtl, cancellationToken);

        foreach (var userId in userIds)
        {
            await matchStore.SetUserMatchAsync(userId, matchId, MatchTtl, cancellationToken);
            await pendingRequestStore.ClearPendingAsync(userId, cancellationToken);
        }

        logger.LogInformation("Match making completed. MatchId: {MatchId}, UserIds: {UserIds}", matchId, string.Join(", ", userIds));
    }
}

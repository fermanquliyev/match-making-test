using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Application.Implementations;

public sealed class MatchFormationService(
    IMatchQueueStore queueStore,
    IMatchCompletePublisher matchCompletePublisher,
    int playersPerMatch,
    ILogger<MatchFormationService> logger) : IMatchFormationService
{
    public async Task TryFormMatchAsync(CancellationToken cancellationToken)
    {
        var userIds = await queueStore.TryDequeueBatchAsync(playersPerMatch, cancellationToken);
        if (userIds is null || userIds.Count != playersPerMatch)
            return;

        var distinct = userIds.Distinct().ToList();
        if (distinct.Count != playersPerMatch)
        {
            logger.LogWarning("Duplicate userIds in batch, re-queuing and skipping match formation");
            await queueStore.RequeueBatchAsync(userIds, cancellationToken);
            return;
        }

        var matchId = Guid.NewGuid().ToString();
        await matchCompletePublisher.PublishAsync(matchId, userIds, DateTime.UtcNow, cancellationToken);
        logger.LogInformation("Match formed. MatchId: {MatchId}, UserIds: {UserIds}", matchId, string.Join(", ", userIds));
    }
}

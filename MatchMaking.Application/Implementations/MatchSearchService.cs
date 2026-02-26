using MatchMaking.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Application.Implementations;

public sealed class MatchSearchService(
    IPendingRequestStore pendingRequestStore,
    IMatchmakingRequestPublisher publisher,
    ILogger<MatchSearchService> logger) : IMatchSearchService
{
    public async Task<MatchSearchResult> RequestMatchSearchAsync(string userId, CancellationToken cancellationToken)
    {
        if (await pendingRequestStore.IsPendingAsync(userId, cancellationToken))
        {
            logger.LogDebug("Duplicate match search request ignored for userId {UserId}", userId);
            return new MatchSearchResult(MatchSearchStatus.AlreadyPending);
        }

        if (!await pendingRequestStore.TrySetPendingAsync(userId, cancellationToken))
        {
            logger.LogDebug("Could not set pending (already pending) for userId {UserId}", userId);
            return new MatchSearchResult(MatchSearchStatus.AlreadyPending);
        }

        await publisher.PublishAsync(userId, cancellationToken);
        logger.LogInformation("Match search request accepted for userId {UserId}", userId);
        return new MatchSearchResult(MatchSearchStatus.Accepted);
    }
}

namespace MatchMaking.Application.Interfaces;

public interface IMatchSearchService
{
    Task<MatchSearchResult> RequestMatchSearchAsync(string userId, CancellationToken cancellationToken = default);
}

public enum MatchSearchStatus
{
    Accepted,
    RateLimited,
    AlreadyPending
}

public sealed record MatchSearchResult(MatchSearchStatus Status);

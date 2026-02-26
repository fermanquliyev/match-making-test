namespace MatchMaking.Application.Interfaces;

public interface IMatchSearchService
{
    Task<MatchSearchResult> RequestMatchSearchAsync(string userId, CancellationToken cancellationToken);
}

public enum MatchSearchStatus
{
    Accepted,
    AlreadyPending
}

public sealed record MatchSearchResult(MatchSearchStatus Status);

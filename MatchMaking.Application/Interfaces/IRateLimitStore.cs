namespace MatchMaking.Application.Interfaces;

public interface IRateLimitStore
{
    Task<bool> TryAcquireAsync(string userId, TimeSpan window, CancellationToken cancellationToken = default);
}

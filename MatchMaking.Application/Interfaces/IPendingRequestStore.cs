namespace MatchMaking.Application.Interfaces;

public interface IPendingRequestStore
{
    Task<bool> TrySetPendingAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsPendingAsync(string userId, CancellationToken cancellationToken = default);
    Task ClearPendingAsync(string userId, CancellationToken cancellationToken = default);
}

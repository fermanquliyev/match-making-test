namespace MatchMaking.Application.Interfaces;

public interface IPendingRequestStore
{
    Task<bool> TrySetPendingAsync(string userId, CancellationToken cancellationToken);
    Task<bool> IsPendingAsync(string userId, CancellationToken cancellationToken);
    Task ClearPendingAsync(string userId, CancellationToken cancellationToken);
}

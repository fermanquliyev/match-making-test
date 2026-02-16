namespace MatchMaking.Application.Interfaces;

public interface IMatchQueueStore
{
    Task EnqueueAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>?> TryDequeueBatchAsync(int count, CancellationToken cancellationToken = default);
    Task RequeueBatchAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken = default);
}

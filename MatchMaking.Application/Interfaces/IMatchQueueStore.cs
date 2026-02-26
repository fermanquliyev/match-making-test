namespace MatchMaking.Application.Interfaces;

public interface IMatchQueueStore
{
    Task EnqueueAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>?> TryDequeueBatchAsync(int count, CancellationToken cancellationToken);
    Task RequeueBatchAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken);
}

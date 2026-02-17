namespace MatchMaking.Application.Interfaces;

public interface IMatchCompletePublisher
{
    Task PublishAsync(string matchId, IReadOnlyList<string> userIds, DateTime createdAtUtc, CancellationToken cancellationToken);
}

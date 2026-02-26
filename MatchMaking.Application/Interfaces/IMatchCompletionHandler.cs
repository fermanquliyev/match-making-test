namespace MatchMaking.Application.Interfaces;

public interface IMatchCompletionHandler
{
    Task HandleMatchCompleteAsync(string matchId, IReadOnlyList<string> userIds, DateTime createdAtUtc, CancellationToken cancellationToken);
}

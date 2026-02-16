namespace MatchMaking.Application.Interfaces;

public interface IMatchmakingRequestPublisher
{
    Task PublishAsync(string userId, CancellationToken cancellationToken = default);
}

namespace MatchMaking.Application.Interfaces;

public interface IMatchFormationService
{
    Task TryFormMatchAsync(CancellationToken cancellationToken);
}

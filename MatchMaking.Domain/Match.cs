namespace MatchMaking.Domain;

public sealed record Match(string MatchId, IReadOnlyList<string> UserIds, DateTime CreatedAtUtc);

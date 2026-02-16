namespace MatchMaking.Contracts;

public sealed record MatchmakingComplete(
    string MatchId,
    IReadOnlyList<string> UserIds,
    DateTime CreatedAtUtc,
    int Version = 1);

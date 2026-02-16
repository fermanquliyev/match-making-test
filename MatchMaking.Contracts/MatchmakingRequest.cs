namespace MatchMaking.Contracts;

public sealed record MatchmakingRequest(
    string UserId,
    DateTime CreatedAtUtc,
    int Version = 1);

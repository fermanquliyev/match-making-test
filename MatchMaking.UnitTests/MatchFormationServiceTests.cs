using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using Xunit;

namespace MatchMaking.UnitTests;

public class MatchFormationServiceTests
{
    [Fact]
    public async Task TryFormMatchAsync_WhenQueueHasEnoughDistinctUsers_FormsMatchAndPublishes()
    {
        var userIds = new List<string> { "u1", "u2", "u3" };
        var queue = new FakeQueueStore { DequeueResult = userIds };
        var publisher = new FakeMatchCompletePublisher();
        var logger = TestLogger<MatchFormationService>.Instance;
        var sut = new MatchFormationService(queue, publisher, 3, logger);

        await sut.TryFormMatchAsync();

        Assert.Single(publisher.Published);
        Assert.Equal(3, publisher.Published[0].UserIds.Count);
        Assert.Equal("u1", publisher.Published[0].UserIds[0]);
        Assert.Equal("u2", publisher.Published[0].UserIds[1]);
        Assert.Equal("u3", publisher.Published[0].UserIds[2]);
    }

    [Fact]
    public async Task TryFormMatchAsync_WhenQueueReturnsNull_DoesNotPublish()
    {
        var queue = new FakeQueueStore { DequeueResult = null };
        var publisher = new FakeMatchCompletePublisher();
        var logger = TestLogger<MatchFormationService>.Instance;
        var sut = new MatchFormationService(queue, publisher, 3, logger);

        await sut.TryFormMatchAsync();

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task TryFormMatchAsync_WhenBatchHasDuplicateUserIds_RequeuesAndDoesNotPublish()
    {
        var userIds = new List<string> { "u1", "u1", "u2" };
        var queue = new FakeQueueStore { DequeueResult = userIds };
        var publisher = new FakeMatchCompletePublisher();
        var logger = TestLogger<MatchFormationService>.Instance;
        var sut = new MatchFormationService(queue, publisher, 3, logger);

        await sut.TryFormMatchAsync();

        Assert.Single(queue.Requeued);
        Assert.Equal(userIds, queue.Requeued[0]);
        Assert.Empty(publisher.Published);
    }

    private sealed class FakeQueueStore : IMatchQueueStore
    {
        public IReadOnlyList<string>? DequeueResult { get; set; }
        public readonly List<IReadOnlyList<string>> Requeued = [];

        public Task EnqueueAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<string>?> TryDequeueBatchAsync(int count, CancellationToken cancellationToken = default) => Task.FromResult(DequeueResult);
        public Task RequeueBatchAsync(IReadOnlyList<string> userIds, CancellationToken cancellationToken = default)
        {
            Requeued.Add(userIds);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMatchCompletePublisher : IMatchCompletePublisher
    {
        public readonly List<(string MatchId, IReadOnlyList<string> UserIds, DateTime CreatedAtUtc)> Published = [];

        public Task PublishAsync(string matchId, IReadOnlyList<string> userIds, DateTime createdAtUtc, CancellationToken cancellationToken = default)
        {
            Published.Add((matchId, userIds, createdAtUtc));
            return Task.CompletedTask;
        }
    }
}

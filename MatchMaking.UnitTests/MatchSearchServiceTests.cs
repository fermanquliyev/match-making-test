using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using Xunit;

namespace MatchMaking.UnitTests;

public class MatchSearchServiceTests
{
    [Fact]
    public async Task RequestMatchSearchAsync_WhenRateLimitFails_ReturnsRateLimited()
    {
        var rateLimit = new FakeRateLimitStore { AcquireResult = false };
        var pending = new FakePendingRequestStore();
        var publisher = new FakeMatchmakingRequestPublisher();
        var logger = TestLogger<MatchSearchService>.Instance;
        var sut = new MatchSearchService(rateLimit, pending, publisher, logger);

        var result = await sut.RequestMatchSearchAsync("user1");

        Assert.Equal(MatchSearchStatus.RateLimited, result.Status);
        Assert.Empty(publisher.PublishedUserIds);
    }

    [Fact]
    public async Task RequestMatchSearchAsync_WhenAlreadyPending_ReturnsAlreadyPendingAndDoesNotPublish()
    {
        var rateLimit = new FakeRateLimitStore { AcquireResult = true };
        var pending = new FakePendingRequestStore { IsPendingResult = true };
        var publisher = new FakeMatchmakingRequestPublisher();
        var logger = TestLogger<MatchSearchService>.Instance;
        var sut = new MatchSearchService(rateLimit, pending, publisher, logger);

        var result = await sut.RequestMatchSearchAsync("user1");

        Assert.Equal(MatchSearchStatus.AlreadyPending, result.Status);
        Assert.Empty(publisher.PublishedUserIds);
    }

    [Fact]
    public async Task RequestMatchSearchAsync_WhenTrySetPendingFails_ReturnsAlreadyPending()
    {
        var rateLimit = new FakeRateLimitStore { AcquireResult = true };
        var pending = new FakePendingRequestStore { IsPendingResult = false, TrySetPendingResult = false };
        var publisher = new FakeMatchmakingRequestPublisher();
        var logger = TestLogger<MatchSearchService>.Instance;
        var sut = new MatchSearchService(rateLimit, pending, publisher, logger);

        var result = await sut.RequestMatchSearchAsync("user1");

        Assert.Equal(MatchSearchStatus.AlreadyPending, result.Status);
        Assert.Empty(publisher.PublishedUserIds);
    }

    [Fact]
    public async Task RequestMatchSearchAsync_WhenSuccess_PublishesAndReturnsAccepted()
    {
        var rateLimit = new FakeRateLimitStore { AcquireResult = true };
        var pending = new FakePendingRequestStore { IsPendingResult = false, TrySetPendingResult = true };
        var publisher = new FakeMatchmakingRequestPublisher();
        var logger = TestLogger<MatchSearchService>.Instance;
        var sut = new MatchSearchService(rateLimit, pending, publisher, logger);

        var result = await sut.RequestMatchSearchAsync("user1");

        Assert.Equal(MatchSearchStatus.Accepted, result.Status);
        Assert.Single(publisher.PublishedUserIds);
        Assert.Equal("user1", publisher.PublishedUserIds[0]);
    }

    private sealed class FakeRateLimitStore : IRateLimitStore
    {
        public bool AcquireResult { get; set; }
        public Task<bool> TryAcquireAsync(string userId, TimeSpan window, CancellationToken cancellationToken = default) => Task.FromResult(AcquireResult);
    }

    private sealed class FakePendingRequestStore : IPendingRequestStore
    {
        public bool IsPendingResult { get; set; }
        public bool TrySetPendingResult { get; set; }
        public Task<bool> TrySetPendingAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(TrySetPendingResult);
        public Task<bool> IsPendingAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(IsPendingResult);
        public Task ClearPendingAsync(string userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeMatchmakingRequestPublisher : IMatchmakingRequestPublisher
    {
        public readonly List<string> PublishedUserIds = [];
        public Task PublishAsync(string userId, CancellationToken cancellationToken = default)
        {
            PublishedUserIds.Add(userId);
            return Task.CompletedTask;
        }
    }
}

using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace MatchMaking.UnitTests;

public class MatchSearchServiceTests
{
    private readonly AutoMocker _mocker = new();

    [Fact]
    public async Task RequestMatchSearchAsync_WhenAlreadyPending_ReturnsAlreadyPendingAndDoesNotPublish()
    {
        _mocker.GetMock<IPendingRequestStore>()
            .Setup(x => x.IsPendingAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = _mocker.CreateInstance<MatchSearchService>();

        var result = await sut.RequestMatchSearchAsync("user1", CancellationToken.None);

        Assert.Equal(MatchSearchStatus.AlreadyPending, result.Status);
        _mocker.GetMock<IMatchmakingRequestPublisher>()
            .Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestMatchSearchAsync_WhenTrySetPendingFails_ReturnsAlreadyPending()
    {
        _mocker.GetMock<IPendingRequestStore>()
            .Setup(x => x.IsPendingAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mocker.GetMock<IPendingRequestStore>()
            .Setup(x => x.TrySetPendingAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = _mocker.CreateInstance<MatchSearchService>();

        var result = await sut.RequestMatchSearchAsync("user1", CancellationToken.None);

        Assert.Equal(MatchSearchStatus.AlreadyPending, result.Status);
        _mocker.GetMock<IMatchmakingRequestPublisher>()
            .Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestMatchSearchAsync_WhenSuccess_PublishesAndReturnsAccepted()
    {
        _mocker.GetMock<IPendingRequestStore>()
            .Setup(x => x.IsPendingAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mocker.GetMock<IPendingRequestStore>()
            .Setup(x => x.TrySetPendingAsync("user1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = _mocker.CreateInstance<MatchSearchService>();

        var result = await sut.RequestMatchSearchAsync("user1", CancellationToken.None);

        Assert.Equal(MatchSearchStatus.Accepted, result.Status);
        _mocker.GetMock<IMatchmakingRequestPublisher>()
            .Verify(x => x.PublishAsync("user1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

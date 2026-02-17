using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace MatchMaking.UnitTests;

public class MatchFormationServiceTests
{
    private readonly AutoMocker _mocker = new();

    private MatchFormationService CreateSut(int playersPerMatch = 3)
    {
        return new MatchFormationService(
            _mocker.Get<IMatchQueueStore>(),
            _mocker.Get<IMatchCompletePublisher>(),
            playersPerMatch,
            _mocker.Get<Microsoft.Extensions.Logging.ILogger<MatchFormationService>>());
    }

    [Fact]
    public async Task TryFormMatchAsync_WhenQueueHasEnoughDistinctUsers_FormsMatchAndPublishes()
    {
        var userIds = new List<string> { "u1", "u2", "u3" };
        _mocker.GetMock<IMatchQueueStore>()
            .Setup(x => x.TryDequeueBatchAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userIds);

        var sut = CreateSut();

        await sut.TryFormMatchAsync(CancellationToken.None);

        _mocker.GetMock<IMatchCompletePublisher>()
            .Verify(x => x.PublishAsync(
                It.IsAny<string>(),
                It.Is<IReadOnlyList<string>>(ids => ids.Count == 3 && ids[0] == "u1" && ids[1] == "u2" && ids[2] == "u3"),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryFormMatchAsync_WhenQueueReturnsNull_DoesNotPublish()
    {
        _mocker.GetMock<IMatchQueueStore>()
            .Setup(x => x.TryDequeueBatchAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string>?)null);

        var sut = CreateSut();

        await sut.TryFormMatchAsync(CancellationToken.None);

        _mocker.GetMock<IMatchCompletePublisher>()
            .Verify(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TryFormMatchAsync_WhenBatchHasDuplicateUserIds_RequeuesAndDoesNotPublish()
    {
        var userIds = new List<string> { "u1", "u1", "u2" };
        _mocker.GetMock<IMatchQueueStore>()
            .Setup(x => x.TryDequeueBatchAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userIds);

        var sut = CreateSut();

        await sut.TryFormMatchAsync(CancellationToken.None);

        _mocker.GetMock<IMatchQueueStore>()
            .Verify(x => x.RequeueBatchAsync(userIds, It.IsAny<CancellationToken>()), Times.Once);
        _mocker.GetMock<IMatchCompletePublisher>()
            .Verify(x => x.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
}

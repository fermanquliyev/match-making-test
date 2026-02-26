using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Application.Interfaces;
using MatchMaking.Contracts;
using MatchMaking.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MatchMaking.IntegrationTests;

public sealed class MatchMakingFlowTests : IClassFixture<MatchMakingApplicationFactory>
{
    private readonly MatchMakingApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public MatchMakingFlowTests(MatchMakingApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    // ────────────────────────────────────────────
    //  Input Validation
    // ────────────────────────────────────────────

    [Fact]
    public async Task Search_WithMissingUserId_Returns400()
    {
        var response = await _client.PostAsync("/api/Match/search", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithEmptyOrWhitespaceUserId_Returns400(string userId)
    {
        var response = await _client.PostAsync($"/api/Match/search?userId={Uri.EscapeDataString(userId)}", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMatch_WithMissingUserId_Returns400()
    {
        var response = await _client.GetAsync("/api/Match");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetMatch_WithEmptyOrWhitespaceUserId_Returns400(string userId)
    {
        var response = await _client.GetAsync($"/api/Match?userId={Uri.EscapeDataString(userId)}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ────────────────────────────────────────────
    //  Search Flow
    // ────────────────────────────────────────────

    [Fact]
    public async Task Search_WithValidUserId_Returns204()
    {
        var response = await _client.PostAsync($"/api/Match/search?userId={UniqueUserId()}", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Search_SameUserTwice_BothReturn204()
    {
        var userId = UniqueUserId();

        var first = await _client.PostAsync($"/api/Match/search?userId={userId}", null);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await _client.PostAsync($"/api/Match/search?userId={userId}", null);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task Search_MultipleDistinctUsers_AllReturn204()
    {
        for (var i = 0; i < 5; i++)
        {
            var response = await _client.PostAsync($"/api/Match/search?userId={UniqueUserId()}", null);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }

    // ────────────────────────────────────────────
    //  GetMatch Flow
    // ────────────────────────────────────────────

    [Fact]
    public async Task GetMatch_WithUnknownUserId_Returns404()
    {
        var response = await _client.GetAsync($"/api/Match?userId={UniqueUserId()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMatch_AfterSearchButNoMatchFormed_Returns404()
    {
        var userId = UniqueUserId();
        await _client.PostAsync($"/api/Match/search?userId={userId}", null);

        var response = await _client.GetAsync($"/api/Match?userId={userId}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMatch_WhenMatchExists_Returns200WithCorrectData()
    {
        var matchId = Guid.NewGuid().ToString();
        var userIds = new List<string> { UniqueUserId(), UniqueUserId(), UniqueUserId() };

        await SeedMatchAsync(matchId, userIds);

        var response = await _client.GetAsync($"/api/Match?userId={userIds[0]}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MatchInfoDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(matchId, body.MatchId);
        Assert.Equal(3, body.UserIds.Count);
        Assert.Contains(userIds[0], body.UserIds);
        Assert.Contains(userIds[1], body.UserIds);
        Assert.Contains(userIds[2], body.UserIds);
    }

    [Fact]
    public async Task GetMatch_AllUsersInMatch_ReturnSameMatchId()
    {
        var matchId = Guid.NewGuid().ToString();
        var userIds = new List<string> { UniqueUserId(), UniqueUserId(), UniqueUserId() };

        await SeedMatchAsync(matchId, userIds);

        foreach (var uid in userIds)
        {
            var response = await _client.GetAsync($"/api/Match?userId={uid}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadFromJsonAsync<MatchInfoDto>(JsonOptions);
            Assert.NotNull(body);
            Assert.Equal(matchId, body.MatchId);
        }
    }

    // ────────────────────────────────────────────
    //  End-to-End Business Flow
    //  (simulates Worker via IMatchCompletionHandler)
    // ────────────────────────────────────────────

    [Fact]
    public async Task FullFlow_SearchThenMatchComplete_GetMatchReturnsMatch()
    {
        var userIds = new[] { UniqueUserId(), UniqueUserId(), UniqueUserId() };

        foreach (var uid in userIds)
        {
            var searchResponse = await _client.PostAsync($"/api/Match/search?userId={uid}", null);
            Assert.Equal(HttpStatusCode.NoContent, searchResponse.StatusCode);
        }

        var matchId = Guid.NewGuid().ToString();
        var handler = _factory.Services.GetRequiredService<IMatchCompletionHandler>();
        await handler.HandleMatchCompleteAsync(matchId, userIds, DateTime.UtcNow, CancellationToken.None);

        foreach (var uid in userIds)
        {
            var getResponse = await _client.GetAsync($"/api/Match?userId={uid}");
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var body = await getResponse.Content.ReadFromJsonAsync<MatchInfoDto>(JsonOptions);
            Assert.NotNull(body);
            Assert.Equal(matchId, body.MatchId);
            Assert.Equal(3, body.UserIds.Count);
        }
    }

    [Fact]
    public async Task FullFlow_AfterMatchComplete_PendingIsCleared_UserCanSearchAgain()
    {
        var userId = UniqueUserId();
        var otherUsers = new[] { UniqueUserId(), UniqueUserId() };
        var allUsers = new[] { userId, otherUsers[0], otherUsers[1] };

        var searchResponse = await _client.PostAsync($"/api/Match/search?userId={userId}", null);
        Assert.Equal(HttpStatusCode.NoContent, searchResponse.StatusCode);

        var matchId = Guid.NewGuid().ToString();
        var handler = _factory.Services.GetRequiredService<IMatchCompletionHandler>();
        await handler.HandleMatchCompleteAsync(matchId, allUsers, DateTime.UtcNow, CancellationToken.None);

        var pendingStore = _factory.Services.GetRequiredService<IPendingRequestStore>();
        var stillPending = await pendingStore.IsPendingAsync(userId, CancellationToken.None);
        Assert.False(stillPending);

        var reSearch = await _client.PostAsync($"/api/Match/search?userId={userId}", null);
        Assert.Equal(HttpStatusCode.NoContent, reSearch.StatusCode);
    }

    [Fact]
    public async Task FullFlow_MatchCompletionIsIdempotent_SecondCallDoesNotOverwrite()
    {
        var userIds = new[] { UniqueUserId(), UniqueUserId(), UniqueUserId() };
        var matchId = Guid.NewGuid().ToString();

        var handler = _factory.Services.GetRequiredService<IMatchCompletionHandler>();
        await handler.HandleMatchCompleteAsync(matchId, userIds, DateTime.UtcNow, CancellationToken.None);

        await handler.HandleMatchCompleteAsync(matchId, userIds, DateTime.UtcNow, CancellationToken.None);

        var response = await _client.GetAsync($"/api/Match?userId={userIds[0]}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MatchInfoDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(matchId, body.MatchId);
    }

    // ────────────────────────────────────────────
    //  Kafka Connectivity
    // ────────────────────────────────────────────

    [Fact]
    public async Task Health_ReportsKafkaAndRedisAsHealthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task Search_PublishesMessageToKafkaRequestTopic()
    {
        var bootstrapServers = GetBootstrapServers();
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = $"test-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        }).Build();
        consumer.Subscribe("matchmaking.request");

        var userId = UniqueUserId();
        var searchResponse = await _client.PostAsync($"/api/Match/search?userId={userId}", null);
        Assert.Equal(HttpStatusCode.NoContent, searchResponse.StatusCode);

        var found = ConsumeUntil(consumer, msg => msg.Contains(userId), TimeSpan.FromSeconds(15));

        Assert.NotNull(found);

        var request = JsonSerializer.Deserialize<MatchmakingRequest>(found, JsonOptions);
        Assert.NotNull(request);
        Assert.Equal(userId, request.UserId);
    }

    [Fact]
    public async Task KafkaConsumer_ProcessesMatchCompleteMessage_MatchBecomesAvailableViaApi()
    {
        var matchId = Guid.NewGuid().ToString();
        var userIds = new[] { UniqueUserId(), UniqueUserId(), UniqueUserId() };
        var message = new MatchmakingComplete(matchId, userIds, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(message, JsonOptions);

        var producer = _factory.Services.GetRequiredService<IProducer<string, string>>();
        await producer.ProduceAsync("matchmaking.complete",
            new Message<string, string> { Key = matchId, Value = json });
        producer.Flush(TimeSpan.FromSeconds(5));

        var matchStore = _factory.Services.GetRequiredService<IMatchStore>();
        var deadline = DateTime.UtcNow.AddSeconds(15);
        Match? match = null;
        while (DateTime.UtcNow < deadline)
        {
            match = await matchStore.GetMatchByUserIdAsync(userIds[0], CancellationToken.None);
            if (match is not null) break;
            await Task.Delay(500);
        }

        Assert.NotNull(match);
        Assert.Equal(matchId, match.MatchId);
        Assert.Equal(3, match.UserIds.Count);

        var response = await _client.GetAsync($"/api/Match?userId={userIds[0]}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MatchInfoDto>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(matchId, body.MatchId);
    }

    // ────────────────────────────────────────────
    //  Helpers
    // ────────────────────────────────────────────

    private static string UniqueUserId() => $"test-{Guid.NewGuid():N}";

    private async Task SeedMatchAsync(string matchId, IReadOnlyList<string> userIds)
    {
        var matchStore = _factory.Services.GetRequiredService<IMatchStore>();
        var match = new Match(matchId, userIds, DateTime.UtcNow);
        await matchStore.SaveMatchAsync(match, TimeSpan.FromMinutes(5), CancellationToken.None);
        foreach (var uid in userIds)
            await matchStore.SetUserMatchAsync(uid, matchId, TimeSpan.FromMinutes(5), CancellationToken.None);
    }

    private string GetBootstrapServers()
    {
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        return config.GetConnectionString("kafka")
            ?? config["Kafka:BootstrapServers"]
            ?? "localhost:9092";
    }

    private static string? ConsumeUntil(
        IConsumer<string, string> consumer,
        Func<string, bool> predicate,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));
            if (result?.Message?.Value is not null && predicate(result.Message.Value))
                return result.Message.Value;
        }
        return null;
    }

    private sealed record MatchInfoDto(string MatchId, List<string> UserIds);
}

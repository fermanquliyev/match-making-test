using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MatchMaking.IntegrationTests;

public sealed class MatchMakingFlowTests : IClassFixture<MatchMakingApplicationFactory>
{
    private readonly MatchMakingApplicationFactory _factory;
    private HttpClient _client = null!;

    public MatchMakingFlowTests(MatchMakingApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithMissingUserId_Returns400()
    {
        var response = await _client.PostAsync("/api/Match/search", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMatch_WithMissingUserId_Returns400()
    {
        var response = await _client.GetAsync("/api/Match");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMatch_WithUnknownUserId_Returns404()
    {
        var response = await _client.GetAsync("/api/Match?userId=nonexistent-user-12345");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithValidUserId_Returns204()
    {
        var userId = $"test-user-{Guid.NewGuid():N}";
        var response = await _client.PostAsync($"/api/Match/search?userId={userId}", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Health_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

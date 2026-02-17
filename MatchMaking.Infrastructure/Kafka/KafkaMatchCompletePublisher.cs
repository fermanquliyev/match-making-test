using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Application.Interfaces;
using MatchMaking.Contracts;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Infrastructure.Kafka;

public sealed class KafkaMatchCompletePublisher(
    IProducer<string, string> producer,
    string topic,
    ILogger<KafkaMatchCompletePublisher> logger) : IMatchCompletePublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task PublishAsync(string matchId, IReadOnlyList<string> userIds, DateTime createdAtUtc, CancellationToken cancellationToken)
    {
        var message = new MatchmakingComplete(matchId, userIds, createdAtUtc);
        var json = JsonSerializer.Serialize(message, JsonOptions);
        try
        {
            await producer.ProduceAsync(topic, new Message<string, string> { Key = matchId, Value = json }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish match complete for matchId {MatchId}", matchId);
            throw;
        }
    }
}

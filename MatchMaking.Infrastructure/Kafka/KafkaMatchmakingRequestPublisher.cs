using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Application.Interfaces;
using MatchMaking.Contracts;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Infrastructure.Kafka;

public sealed class KafkaMatchmakingRequestPublisher(
    IProducer<string, string> producer,
    string topic,
    ILogger<KafkaMatchmakingRequestPublisher> logger) : IMatchmakingRequestPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task PublishAsync(string userId, CancellationToken cancellationToken = default)
    {
        var message = new MatchmakingRequest(userId, DateTime.UtcNow);
        var json = JsonSerializer.Serialize(message, JsonOptions);
        try
        {
            await producer.ProduceAsync(topic, new Message<string, string> { Key = userId, Value = json }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish matchmaking request for userId {UserId}", userId);
            throw;
        }
    }
}

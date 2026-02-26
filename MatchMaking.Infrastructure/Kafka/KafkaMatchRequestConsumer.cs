using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Application.Interfaces;
using MatchMaking.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Infrastructure.Kafka;

public sealed class KafkaMatchRequestConsumer(
    IConsumer<string, string> consumer,
    IMatchQueueStore queueStore,
    IMatchFormationService matchFormationService,
    ILogger<KafkaMatchRequestConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result.Message?.Value is null)
                    {
                        consumer.Commit(result);
                        continue;
                    }

                    var request = JsonSerializer.Deserialize<MatchmakingRequest>(result.Message.Value, JsonOptions);
                    if (request is null)
                    {
                        logger.LogWarning("Invalid matchmaking.request message, skipping");
                        consumer.Commit(result);
                        continue;
                    }

                    await queueStore.EnqueueAsync(request.UserId, stoppingToken);
                    await matchFormationService.TryFormMatchAsync(stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming matchmaking.request");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}

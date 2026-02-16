using System.Text.Json;
using Confluent.Kafka;
using MatchMaking.Application.Interfaces;
using MatchMaking.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MatchMaking.Infrastructure.Kafka;

public sealed class KafkaMatchCompleteConsumer(
    IConsumer<string, string> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<KafkaMatchCompleteConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result.Message?.Value is null) continue;

                    var complete = JsonSerializer.Deserialize<MatchmakingComplete>(result.Message.Value, JsonOptions);
                    if (complete is null)
                    {
                        logger.LogWarning("Invalid matchmaking.complete message, skipping");
                        consumer.Commit(result);
                        continue;
                    }

                    using (var scope = scopeFactory.CreateScope())
                    {
                        var handler = scope.ServiceProvider.GetRequiredService<IMatchCompletionHandler>();
                        await handler.HandleMatchCompleteAsync(complete.MatchId, complete.UserIds, complete.CreatedAtUtc, stoppingToken);
                    }
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming matchmaking.complete");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}

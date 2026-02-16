using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MatchMaking.Service.Health;

public sealed class KafkaHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? configuration.GetConnectionString("kafka")
            ?? "localhost:9092";
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
            return Task.FromResult(metadata.Brokers.Count > 0
                ? HealthCheckResult.Healthy("Kafka is available")
                : HealthCheckResult.Unhealthy("No brokers found"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Kafka check failed", ex));
        }
    }
}

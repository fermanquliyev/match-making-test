using Microsoft.Extensions.Configuration;

namespace MatchMaking.Infrastructure;

/// <summary>
/// Resolves configuration in a way that works with both Aspire (WithReference) and Docker Compose (environment variables).
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Redis: Aspire injects ConnectionStrings:redis (lowercase); Docker uses ConnectionStrings__Redis.
    /// .NET config is case-insensitive, so GetConnectionString("Redis") works for both.
    /// </summary>
    public static string GetRedisConnectionString(this IConfiguration configuration) =>
        configuration.GetConnectionString("Redis")
        ?? configuration.GetConnectionString("redis")
        ?? "localhost:6379";

    /// <summary>
    /// Kafka: Aspire may inject ConnectionStrings:kafka or Kafka:BootstrapServers; Docker uses Kafka__BootstrapServers.
    /// </summary>
    public static string GetKafkaBootstrapServers(this IConfiguration configuration) =>
        configuration["Kafka:BootstrapServers"]
        ?? configuration.GetConnectionString("kafka")
        ?? "localhost:9092";
}

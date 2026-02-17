using Confluent.Kafka;
using MatchMaking.Application.Implementations;
using MatchMaking.Application.Interfaces;
using MatchMaking.Contracts;
using MatchMaking.Infrastructure.Kafka;
using MatchMaking.Infrastructure.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MatchMaking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetRedisConnectionString();
        var options = ConfigurationOptions.Parse(redisConnection);
        options.AbortOnConnectFail = false; // Allow startup when Redis is not ready yet (e.g. Aspire starting the container)
        var redis = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(redis);

        services.AddSingleton<IPendingRequestStore, RedisPendingRequestStore>();
        services.AddSingleton<IMatchStore, RedisMatchStore>();
        services.AddSingleton<IMatchQueueStore, RedisMatchQueueStore>();

        var bootstrapServers = configuration.GetKafkaBootstrapServers();
        var requestTopic = configuration["Kafka:Topics:Request"] ?? "matchmaking.request";
        var completeTopic = configuration["Kafka:Topics:Complete"] ?? "matchmaking.complete";

        services.AddSingleton<IProducer<string, string>>(_ =>
        {
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            return new ProducerBuilder<string, string>(config).Build();
        });

        services.AddSingleton<IMatchmakingRequestPublisher>(sp =>
        {
            var producer = sp.GetRequiredService<IProducer<string, string>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KafkaMatchmakingRequestPublisher>>();
            return new KafkaMatchmakingRequestPublisher(producer, requestTopic, logger);
        });

        services.AddSingleton<IMatchCompletePublisher>(sp =>
        {
            var producer = sp.GetRequiredService<IProducer<string, string>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KafkaMatchCompletePublisher>>();
            return new KafkaMatchCompletePublisher(producer, completeTopic, logger);
        });

        return services;
    }

    public static IServiceCollection AddServiceKafkaConsumer(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration.GetKafkaBootstrapServers();
        var completeTopic = configuration["Kafka:Topics:Complete"] ?? "matchmaking.complete";
        var groupId = configuration["Kafka:ConsumerGroup:Service"] ?? "matchmaking-service";

        services.AddSingleton<IConsumer<string, string>>(_ =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(completeTopic);
            return consumer;
        });
        services.AddHostedService<KafkaMatchCompleteConsumer>();
        return services;
    }

    public static IServiceCollection AddWorkerKafkaConsumer(this IServiceCollection services, IConfiguration configuration)
    {
        var bootstrapServers = configuration.GetKafkaBootstrapServers();
        var requestTopic = configuration["Kafka:Topics:Request"] ?? "matchmaking.request";
        var groupId = configuration["Kafka:ConsumerGroup:Worker"] ?? "matchmaking-worker";

        services.AddSingleton<IConsumer<string, string>>(_ =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                EnableAutoCommit = false,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
            var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(requestTopic);
            return consumer;
        });
        services.AddHostedService<KafkaMatchRequestConsumer>();
        return services;
    }

    public static IServiceCollection AddMatchFormation(this IServiceCollection services, IConfiguration configuration)
    {
        var playersPerMatch = configuration.GetValue("MatchMaking:PlayersPerMatch", 3);
        services.AddSingleton<IMatchFormationService>(sp =>
        {
            var queue = sp.GetRequiredService<IMatchQueueStore>();
            var publisher = sp.GetRequiredService<IMatchCompletePublisher>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MatchFormationService>>();
            return new MatchFormationService(queue, publisher, playersPerMatch, logger);
        });
        return services;
    }
}

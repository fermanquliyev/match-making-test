using MatchMaking.Application;
using MatchMaking.Infrastructure;
using MatchMaking.Service.Health;
using MatchMaking.Service.Middleware;

namespace MatchMaking.Service;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        if (!builder.Environment.IsEnvironment("Testing"))
            builder.Services.AddServiceKafkaConsumer(builder.Configuration);

        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        builder.Services.AddHealthChecks()
            .AddRedis(builder.Configuration.GetRedisConnectionString(), name: "redis")
            .AddCheck<KafkaHealthCheck>("kafka");

        var app = builder.Build();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.MapDefaultEndpoints();

        // Ensure /health is always available for Docker/orchestrator healthchecks (MapDefaultEndpoints only maps it in Development)
        if (!app.Environment.IsDevelopment())
            app.MapHealthChecks("/health");

        app.MapControllers();

        if (app.Environment.IsDevelopment())
            app.MapOpenApi();

        app.Run();
    }
}

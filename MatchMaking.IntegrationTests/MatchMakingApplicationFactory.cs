using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace MatchMaking.IntegrationTests;

/// <summary>
/// Uses configuration from environment or appsettings. Ensure Redis and Kafka are running (e.g. docker compose up -d redis kafka).
/// </summary>
public sealed class MatchMakingApplicationFactory : WebApplicationFactory<MatchMaking.Service.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = Environment.GetEnvironmentVariable("ConnectionStrings__Redis") ?? "localhost:6379",
                ["Kafka:BootstrapServers"] = Environment.GetEnvironmentVariable("Kafka__BootstrapServers") ?? "localhost:9092"
            });
        });
    }
}

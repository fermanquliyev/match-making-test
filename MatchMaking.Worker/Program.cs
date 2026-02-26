using MatchMaking.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddWorkerKafkaConsumer(builder.Configuration);
builder.Services.AddMatchFormation(builder.Configuration);

var host = builder.Build();
host.Run();

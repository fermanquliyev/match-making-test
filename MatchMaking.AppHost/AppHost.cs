var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
var kafka = builder.AddKafka("kafka");

builder.AddProject<Projects.MatchMaking_Service>("matchmaking-service")
    .WithReference(redis)
    .WithReference(kafka)
    .WaitFor(redis)
    .WaitFor(kafka);

builder.AddProject<Projects.MatchMaking_Worker>("matchmaking-worker")
    .WithReplicas(2)
    .WithReference(redis)
    .WithReference(kafka)
    .WaitFor(redis)
    .WaitFor(kafka);

builder.Build().Run();

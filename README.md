# MatchMaking

A distributed matchmaking solution that groups players into matches via an HTTP API (MatchMaking.Service) and a Kafka-based worker (MatchMaking.Worker). The solution uses **.NET Aspire** for local orchestration and includes **docker-compose** for deployment.

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) and Docker Compose (for full-stack or production-style runs)

## Solution file

- Use **MatchMaking.slnx** (Aspire solution). All projects are in this solution.

## Docker Compose vs .NET Aspire

The same codebase runs in both environments:

| | **Docker Compose** | **.NET Aspire** |
|---|---|---|
| **Config** | Env vars: `ConnectionStrings__Redis`, `Kafka__BootstrapServers` | Injected by AppHost via `WithReference(redis)` / `WithReference(kafka)` |
| **Health** | `/health` is always mapped so the container healthcheck works | `/health` and `/alive` in Development via `MapDefaultEndpoints()` |
| **Service** | Builds with ServiceDefaults; runs without Aspire dashboard | Uses AddServiceDefaults(), OpenTelemetry, service discovery |

Redis and Kafka configuration is resolved so that both Aspire (e.g. `ConnectionStrings:redis`) and Docker (e.g. `ConnectionStrings:Redis`) work.

## How to Run with .NET Aspire (recommended for local development)

1. **Start the AppHost** (orchestrates Service, Worker, Redis, and Kafka):

   ```bash
   dotnet run --project MatchMaking.AppHost
   ```

2. Open the Aspire dashboard (URL shown in the console), then use the **matchmaking-service** endpoint to call the API.

3. **Request a match search** and **retrieve match information** (same behavior as below; port is assigned by Aspire):

   - Match Search: `POST /api/Match/search?userId=...`
   - Get Match: `GET /api/Match?userId=...`

4. The Worker runs with **2 replicas** by default when using the AppHost.

## Architecture

- **MatchMaking.Service**: HTTP API for match search requests and match retrieval. Uses Redis for rate limiting and state; produces to Kafka `matchmaking.request` and consumes from `matchmaking.complete`.
- **MatchMaking.Worker**: Consumes `matchmaking.request`, enqueues users in a Redis FIFO queue, and when enough players are available (configurable, default 3), forms a match and publishes to `matchmaking.complete`.

## How to Run with Docker Compose

1. **Start the infrastructure** (Kafka, Zookeeper, Redis, Service, 2 Workers):

   ```bash
   docker compose up -d
   ```

2. **Wait for services to be healthy** (Kafka can take ~30s to start):

   ```bash
   docker compose ps
   ```

3. **Request a match search** (rate limit: 1 request per 100ms per userId):

   ```bash
   curl -X POST "http://localhost:8080/api/Match/search?userId=user1"
   # 204 No Content = accepted (or already pending, idempotent)
   # 429 = too many requests
   # 400 = missing/invalid userId
   ```

4. **Retrieve match information** (after a match is formed for that user):

   ```bash
   curl "http://localhost:8080/api/Match?userId=user1"
   # 200 + JSON body with matchId and userIds when match is ready
   # 404 when no match yet (or invalid userId)
   ```

### Example: Forming a match (3 players by default)

```bash
curl -X POST "http://localhost:8080/api/Match/search?userId=alice"
curl -X POST "http://localhost:8080/api/Match/search?userId=bob"
curl -X POST "http://localhost:8080/api/Match/search?userId=carol"
# After a short delay, each can retrieve the same match:
curl "http://localhost:8080/api/Match?userId=alice"
# {"matchId":"...", "userIds":["alice","bob","carol"]}
```

## How to Run Locally (without Aspire)

1. **Start Redis and Kafka** (e.g. with Docker):

   ```bash
   docker run -d -p 6379:6379 redis:7-alpine
   docker compose up -d zookeeper kafka
   ```

2. **Run the Service**:

   ```bash
   dotnet run --project MatchMaking.Service
   ```
   Service listens on the port configured in `MatchMaking.Service/Properties/launchSettings.json` (or 5000 by default).

3. **Run the Worker** (one or more instances):

   ```bash
   dotnet run --project MatchMaking.Worker
   ```

4. Use the same `curl` commands as above (adjust port if not 8080).

## Configuration

- **MatchMaking.Service** (`appsettings.json`):
  - `ConnectionStrings:Redis`: Redis connection string (default `localhost:6379`).
  - `Kafka:BootstrapServers` (default `localhost:9092`).
  - `Kafka:Topics:Request` / `Complete`: topic names.

- **MatchMaking.Worker** (`appsettings.json`):
  - Same Redis and Kafka settings.
  - `MatchMaking:PlayersPerMatch`: number of users per match (default `3`).

## Solution Structure (Clean Architecture + Aspire)

- **MatchMaking.AppHost**: .NET Aspire host; adds Redis, Kafka, Service, and Worker (2 replicas) and wires references.
- **MatchMaking.ServiceDefaults**: Shared Aspire defaults (health checks, OpenTelemetry, service discovery, resilience) used by the Service.
- **MatchMaking.Domain**: Domain model (e.g. `Match`).
- **MatchMaking.Contracts**: Shared Kafka message contracts (`MatchmakingRequest`, `MatchmakingComplete`).
- **MatchMaking.Application**: Use cases and interfaces (match search, match query, rate limit, queue, store).
- **MatchMaking.Infrastructure**: Redis and Kafka implementations.
- **MatchMaking.Service**: API layer (controllers, middleware, health checks); uses ServiceDefaults and Aspire-injected Redis/Kafka.
- **MatchMaking.Worker**: Host that runs Kafka consumer and match formation.

## Health Checks

- **Service**: `GET http://localhost:8080/health` — checks Redis and Kafka.

## Testing

- **Unit tests** (match grouping, rate limiting, match search flow):
  ```bash
  dotnet test MatchMaking.UnitTests/MatchMaking.UnitTests.csproj -c Release
  ```
- **Integration tests** (HTTP API; require Redis and Kafka running, e.g. `docker compose up -d redis kafka`):
  ```bash
  dotnet test MatchMaking.IntegrationTests/MatchMaking.IntegrationTests.csproj -c Release
  ```

**Build the whole solution (slnx):**
```bash
dotnet build MatchMaking.slnx -c Release
```

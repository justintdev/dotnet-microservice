# Catalog Microservice (.NET 10)

Clean-architecture sample microservice with:
- Auth0 JWT auth
- Custom JSON `ILogger` logs to stdout
- Kafka producer/consumer
- SQLite persistence (local file)
- Redis caching
- Feature flags
- Liveness/readiness endpoints for Kubernetes
- OpenAPI/Swagger
- xUnit + Moq unit tests

## Solution structure
- `Microservice/` API host and controllers
- `Application/` use cases, DTOs, and service contracts
- `Domain/` core entities
- `Infrastructure/` data, messaging, caching, health checks, logging formatter
- `Microservice.Tests/` unit tests

## Auth0 configuration
`Microservice/appsettings.json` is configured with:
- Domain: `dev-w6r6q15ny6i4p6yc.us.auth0.com`
- Audience: `https://catalog-api`

Update audience if your Auth0 API identifier differs.

## Run
```bash
dotnet restore CatalogMicroservice.slnx
dotnet run --project Microservice/Microservice.csproj
```

## Endpoints
- Swagger UI: `http://localhost:5164/swagger`
- OpenAPI JSON: `http://localhost:5164/swagger/v1/swagger.json`
- Liveness: `http://localhost:5164/health/live`
- Readiness: `http://localhost:5164/health/ready`

## Persistence and dependencies
- SQLite file: `Microservice/data/catalog.db` (created automatically)
- Redis: `localhost:6379`
- Kafka: `localhost:9092`, topic `catalog-items`

## Feature flags
Configured in `Microservice/appsettings*.json`:
- `EnableKafkaPublishing`
- `EnableRedisCaching`

## Tests
```bash
dotnet test Microservice.Tests/Microservice.Tests.csproj
```

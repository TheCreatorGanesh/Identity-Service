# Docker Support for Identity Service API

This folder contains all Docker-related files for containerizing the decoupled, configuration-driven Identity Service API.

## Files

- **Dockerfile** (`src/IdentityService.API/Dockerfile`) - Multi-stage Docker build for the .NET 10 Auth API
- **docker-compose.yml** - Orchestration of Auth API (port 8081) and PostgreSQL services (port 5432)
- **docker-compose.local.yml** - Spawns only PostgreSQL in Docker for local host API debugging
- **.dockerignore** - Excludes build outputs, coverage logs, and git artifacts

## Quick Start

### Option 1: Full Docker Orchestration

```bash
docker compose -f auth/docker/docker-compose.yml up -d
```

Access:
- **API URL**: http://localhost:8081
- **OpenAPI / Scalar UI**: http://localhost:8081/scalar/v1
- **Swagger UI**: http://localhost:8081/swagger
- **Health Check**: http://localhost:8081/health

### Option 2: Local Development (Postgres in Docker, API on Host)

```bash
docker compose -f auth/docker/docker-compose.local.yml up -d
```

Then run the Auth API:

```bash
cd auth/src/IdentityService.API
dotnet run
```

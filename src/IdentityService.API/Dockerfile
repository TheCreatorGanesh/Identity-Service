# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6 AS build
WORKDIR /app

# Copy project files for layer-cached restore
COPY Directory.Build.props ./
COPY src/IdentityService.Core/IdentityService.Core.csproj ./src/IdentityService.Core/
COPY src/IdentityService.Application/IdentityService.Application.csproj ./src/IdentityService.Application/
COPY src/IdentityService.Infrastructure/IdentityService.Infrastructure.csproj ./src/IdentityService.Infrastructure/
COPY src/IdentityService.API/IdentityService.API.csproj ./src/IdentityService.API/

# Restore dependencies
RUN dotnet restore src/IdentityService.API/IdentityService.API.csproj

# Copy all source files
COPY src/ ./src/

# Build the API project
RUN dotnet build src/IdentityService.API/IdentityService.API.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish src/IdentityService.API/IdentityService.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Expose API port
EXPOSE 8080
EXPOSE 8443

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "IdentityService.API.dll"]

# =============================================================================
# pickadate.me Backend - Production Dockerfile
# =============================================================================
# Multi-stage build: restore -> build -> publish -> runtime
# Base: .NET 10 (matches Directory.Build.props TargetFramework)
# Port: 8080 (non-root default)
# =============================================================================

# --- Stage 1: Build ---
FROM mcr.microsoft.com/dotnet/sdk:10.0.102 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY Directory.Build.props .
COPY PickadateBackend.slnx .
COPY src/BuildingBlocks/Pickadate.BuildingBlocks.Domain/Pickadate.BuildingBlocks.Domain.csproj src/BuildingBlocks/Pickadate.BuildingBlocks.Domain/
COPY src/BuildingBlocks/Pickadate.BuildingBlocks.Application/Pickadate.BuildingBlocks.Application.csproj src/BuildingBlocks/Pickadate.BuildingBlocks.Application/
COPY src/BuildingBlocks/Pickadate.BuildingBlocks.Infrastructure/Pickadate.BuildingBlocks.Infrastructure.csproj src/BuildingBlocks/Pickadate.BuildingBlocks.Infrastructure/
COPY src/Domain/Pickadate.Domain/Pickadate.Domain.csproj src/Domain/Pickadate.Domain/
COPY src/Application/Pickadate.Application/Pickadate.Application.csproj src/Application/Pickadate.Application/
COPY src/Infrastructure/Pickadate.Infrastructure/Pickadate.Infrastructure.csproj src/Infrastructure/Pickadate.Infrastructure/
COPY src/API/Pickadate.API/Pickadate.API.csproj src/API/Pickadate.API/

RUN dotnet restore src/API/Pickadate.API/Pickadate.API.csproj

# Copy source and publish
COPY src/ src/
RUN dotnet publish src/API/Pickadate.API/Pickadate.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# --- Stage 2: Runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Security: run as non-root (use built-in 'app' user from .NET base image)
USER app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "Pickadate.API.dll"]

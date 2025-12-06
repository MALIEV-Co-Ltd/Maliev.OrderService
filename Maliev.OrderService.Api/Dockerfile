# syntax=docker/dockerfile:1.4
# Multi-stage Docker build for Maliev.OrderService.Api  
# Based on MALIEV Co. Ltd. standard .NET 10 microservice template

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config and project files
COPY nuget.config ./
COPY Maliev.OrderService.Api/Maliev.OrderService.Api.csproj Maliev.OrderService.Api/
COPY Maliev.OrderService.Data/Maliev.OrderService.Data.csproj Maliev.OrderService.Data/

# Restore with GitHub Packages authentication using BuildKit secrets
RUN --mount=type=secret,id=nuget_username \
  --mount=type=secret,id=nuget_password \
  NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
  NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
  dotnet restore Maliev.OrderService.Api/Maliev.OrderService.Api.csproj

# Copy source code
COPY Maliev.OrderService.Api/ Maliev.OrderService.Api/
COPY Maliev.OrderService.Data/ Maliev.OrderService.Data/

# Build and publish
WORKDIR /src/Maliev.OrderService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install PostgreSQL client for health checks
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

# Ensure 'app' owns the workdir (app user already exists in ASP.NET runtime image)
RUN chown -R app:app /app

# Switch to non-root user
USER app

# Copy published files (now owned by app)
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/orders/liveness || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Maliev.OrderService.Api.dll"]

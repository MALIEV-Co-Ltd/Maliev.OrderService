# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY Maliev.OrderService.sln .
COPY Maliev.OrderService.Api/Maliev.OrderService.Api.csproj Maliev.OrderService.Api/
COPY Maliev.OrderService.Data/Maliev.OrderService.Data.csproj Maliev.OrderService.Data/

# Restore dependencies
RUN dotnet restore Maliev.OrderService.sln

# Copy source code
COPY Maliev.OrderService.Api/ Maliev.OrderService.Api/
COPY Maliev.OrderService.Data/ Maliev.OrderService.Data/

# Build and publish
WORKDIR /src/Maliev.OrderService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install PostgreSQL client for health checks
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=build /app/publish .

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

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

# syntax=docker/dockerfile:1.7

# The deployment lane must publish the validated application into /publish before
# building this runtime-only image. Dependency restore never receives image credentials.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

ARG SOURCE_REVISION=unknown
LABEL org.opencontainers.image.source="https://github.com/MALIEV-Co-Ltd/Maliev.OrderService" \
      org.opencontainers.image.revision="${SOURCE_REVISION}"

WORKDIR /app
COPY --chown=app:app publish/ .

USER app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Maliev.OrderService.Api.dll"]

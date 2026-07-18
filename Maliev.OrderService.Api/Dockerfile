# syntax=docker/dockerfile:1.7

ARG dependency_restore_stage=restore-private

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-base
WORKDIR /src

COPY ["nuget.config", "."]
COPY ["NuGet.PRValidation.Config", "."]
COPY ["Directory.Build.props", "."]
COPY ["Maliev.OrderService.Api/Maliev.OrderService.Api.csproj", "Maliev.OrderService.Api/"]
COPY ["Maliev.OrderService.Domain/Maliev.OrderService.Domain.csproj", "Maliev.OrderService.Domain/"]
COPY ["Maliev.OrderService.Application/Maliev.OrderService.Application.csproj", "Maliev.OrderService.Application/"]
COPY ["Maliev.OrderService.Infrastructure/Maliev.OrderService.Infrastructure.csproj", "Maliev.OrderService.Infrastructure/"]
COPY [".ci-packages/", ".ci-packages/"]

FROM build-base AS restore-private
RUN --mount=type=secret,id=nuget_username,required=true \
    --mount=type=secret,id=nuget_password,required=true \
    NUGET_USERNAME="$(cat /run/secrets/nuget_username)" \
    NUGET_PASSWORD="$(cat /run/secrets/nuget_password)" \
    dotnet restore "Maliev.OrderService.Api/Maliev.OrderService.Api.csproj" \
      /p:GITHUB_ACTIONS=true \
      /p:ServiceDefaultsVersion="1.0.81-alpha" \
      /p:MessagingContractsVersion="1.0.91-alpha"

FROM build-base AS restore-local
RUN dotnet restore "Maliev.OrderService.Api/Maliev.OrderService.Api.csproj" \
      --configfile "NuGet.PRValidation.Config" \
      /p:GITHUB_ACTIONS=true \
      /p:ServiceDefaultsVersion="1.0.81-alpha" \
      /p:MessagingContractsVersion="1.0.91-alpha"

FROM ${dependency_restore_stage} AS build
ARG BUILD_CONFIGURATION=Release
COPY . .
WORKDIR "/src/Maliev.OrderService.Api"
RUN dotnet build "Maliev.OrderService.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/build \
    --no-restore \
    /p:GITHUB_ACTIONS=true \
    /p:ServiceDefaultsVersion="1.0.81-alpha" \
    /p:MessagingContractsVersion="1.0.91-alpha"

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Maliev.OrderService.Api.csproj" \
    --configuration "$BUILD_CONFIGURATION" \
    --output /app/publish \
    --no-restore \
    /p:GITHUB_ACTIONS=true \
    /p:ServiceDefaultsVersion="1.0.81-alpha" \
    /p:MessagingContractsVersion="1.0.91-alpha" \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --chown=app:app --from=publish /app/publish .

USER app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Maliev.OrderService.Api.dll"]

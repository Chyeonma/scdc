# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/Community/SCDC.Community.Api/SCDC.Community.Api.csproj", "services/Community/SCDC.Community.Api/"]
COPY ["services/Community/SCDC.Community.Application/SCDC.Community.Application.csproj", "services/Community/SCDC.Community.Application/"]
COPY ["services/Community/SCDC.Community.Domain/SCDC.Community.Domain.csproj", "services/Community/SCDC.Community.Domain/"]
COPY ["services/Community/SCDC.Community.Infrastructure/SCDC.Community.Infrastructure.csproj", "services/Community/SCDC.Community.Infrastructure/"]
COPY ["services/Community/SCDC.Community.Grpc/SCDC.Community.Grpc.csproj", "services/Community/SCDC.Community.Grpc/"]
COPY ["services/Identity/SCDC.Identity.Grpc/SCDC.Identity.Grpc.csproj", "services/Identity/SCDC.Identity.Grpc/"]
COPY ["services/Messaging/SCDC.Messaging.Grpc/SCDC.Messaging.Grpc.csproj", "services/Messaging/SCDC.Messaging.Grpc/"]
COPY ["services/SCDC.Contracts/SCDC.Contracts.csproj", "services/SCDC.Contracts/"]
COPY ["services/SCDC.BuildingBlocks/SCDC.BuildingBlocks.csproj", "services/SCDC.BuildingBlocks/"]
COPY ["services/Shared/SCDC.Shared.Core/SCDC.Shared.Core.csproj", "services/Shared/SCDC.Shared.Core/"]
COPY docs/contracts/grpc/ docs/contracts/grpc/
RUN dotnet restore services/Community/SCDC.Community.Api/SCDC.Community.Api.csproj
COPY . .
RUN dotnet publish services/Community/SCDC.Community.Api/SCDC.Community.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080 8443
COPY --from=build /app/publish .
RUN mkdir -p /app/community-keys /app/auth-public && chown -R "$APP_UID:$APP_UID" /app/community-keys /app/auth-public && chmod 700 /app/community-keys
USER $APP_UID
ENTRYPOINT ["dotnet", "SCDC.Community.Api.dll"]

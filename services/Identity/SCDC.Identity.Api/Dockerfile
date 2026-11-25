# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/Community/SCDC.Community.Grpc/SCDC.Community.Grpc.csproj", "services/Community/SCDC.Community.Grpc/"]
COPY ["services/Identity/SCDC.Identity.Api/SCDC.Identity.Api.csproj", "services/Identity/SCDC.Identity.Api/"]
COPY ["services/Identity/SCDC.Identity.Application/SCDC.Identity.Application.csproj", "services/Identity/SCDC.Identity.Application/"]
COPY ["services/Identity/SCDC.Identity.Domain/SCDC.Identity.Domain.csproj", "services/Identity/SCDC.Identity.Domain/"]
COPY ["services/Identity/SCDC.Identity.Grpc/SCDC.Identity.Grpc.csproj", "services/Identity/SCDC.Identity.Grpc/"]
COPY ["services/Identity/SCDC.Identity.Infrastructure/SCDC.Identity.Infrastructure.csproj", "services/Identity/SCDC.Identity.Infrastructure/"]
COPY ["services/Messaging/SCDC.Messaging.Grpc/SCDC.Messaging.Grpc.csproj", "services/Messaging/SCDC.Messaging.Grpc/"]
COPY ["services/SCDC.Contracts/SCDC.Contracts.csproj", "services/SCDC.Contracts/"]
COPY ["services/Shared/SCDC.Shared.Core/SCDC.Shared.Core.csproj", "services/Shared/SCDC.Shared.Core/"]
COPY docs/contracts/grpc/ docs/contracts/grpc/
RUN dotnet restore "services/Identity/SCDC.Identity.Api/SCDC.Identity.Api.csproj"
COPY . .
RUN dotnet publish "services/Identity/SCDC.Identity.Api/SCDC.Identity.Api.csproj" -c Release --no-restore -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080 8443
COPY --from=build /app/publish .
RUN mkdir -p /app/.dm-keys/user-search /app/messaging-cursor-keys /app/community-keys /app/messaging-keys /app/auth-private /app/auth-public && chown -R "$APP_UID:$APP_UID" /app/.dm-keys/user-search /app/messaging-cursor-keys /app/community-keys /app/messaging-keys /app/auth-private /app/auth-public && chmod 700 /app/.dm-keys/user-search /app/messaging-cursor-keys /app/auth-private
USER $APP_UID
ENTRYPOINT ["dotnet", "SCDC.Identity.Api.dll"]

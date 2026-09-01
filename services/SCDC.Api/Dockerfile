# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["services/SCDC.BuildingBlocks/SCDC.BuildingBlocks.csproj", "services/SCDC.BuildingBlocks/"]
COPY ["services/SCDC.Contracts/SCDC.Contracts.csproj", "services/SCDC.Contracts/"]
COPY ["services/Modules/Identity/SCDC.Identity.csproj", "services/Modules/Identity/"]
COPY ["services/Modules/Community/SCDC.Community.csproj", "services/Modules/Community/"]
COPY ["services/Modules/Messaging/SCDC.Messaging.csproj", "services/Modules/Messaging/"]
COPY ["services/SCDC.Api/SCDC.Api.csproj", "services/SCDC.Api/"]
RUN dotnet restore "services/SCDC.Api/SCDC.Api.csproj"

COPY . .
RUN dotnet publish "services/SCDC.Api/SCDC.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0

EXPOSE 8080

COPY --from=build /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "SCDC.Api.dll"]

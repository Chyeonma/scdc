# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["services/ChatService.Domain/ChatService.Domain.csproj", "services/ChatService.Domain/"]
COPY ["services/ChatService.Application/ChatService.Application.csproj", "services/ChatService.Application/"]
COPY ["services/ChatService.Infrastructure/ChatService.Infrastructure.csproj", "services/ChatService.Infrastructure/"]
COPY ["services/ChatService/ChatService.csproj", "services/ChatService/"]
RUN dotnet restore "services/ChatService/ChatService.csproj"

COPY . .
RUN dotnet publish "services/ChatService/ChatService.csproj" \
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

ENTRYPOINT ["dotnet", "ChatService.dll"]

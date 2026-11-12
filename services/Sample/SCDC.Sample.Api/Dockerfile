# syntax=docker/dockerfile:1
# Build context: services/Sample. This image builds without the monolith.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SCDC.Sample.Domain/SCDC.Sample.Domain.csproj", "SCDC.Sample.Domain/"]
COPY ["SCDC.Sample.Application/SCDC.Sample.Application.csproj", "SCDC.Sample.Application/"]
COPY ["SCDC.Sample.Infrastructure/SCDC.Sample.Infrastructure.csproj", "SCDC.Sample.Infrastructure/"]
COPY ["SCDC.Sample.Api/SCDC.Sample.Api.csproj", "SCDC.Sample.Api/"]
RUN dotnet restore SCDC.Sample.Api/SCDC.Sample.Api.csproj
COPY . .
RUN dotnet publish SCDC.Sample.Api/SCDC.Sample.Api.csproj \
    --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SCDC.Sample.Api.dll"]

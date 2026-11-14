# syntax=docker/dockerfile:1
# Build context: services. Only Sample and Shared.Core are copied; no monolith project is built.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Shared/SCDC.Shared.Core/SCDC.Shared.Core.csproj", "Shared/SCDC.Shared.Core/"]
COPY ["Sample/SCDC.Sample.Domain/SCDC.Sample.Domain.csproj", "Sample/SCDC.Sample.Domain/"]
COPY ["Sample/SCDC.Sample.Application/SCDC.Sample.Application.csproj", "Sample/SCDC.Sample.Application/"]
COPY ["Sample/SCDC.Sample.Infrastructure/SCDC.Sample.Infrastructure.csproj", "Sample/SCDC.Sample.Infrastructure/"]
COPY ["Sample/SCDC.Sample.Api/SCDC.Sample.Api.csproj", "Sample/SCDC.Sample.Api/"]
RUN dotnet restore Sample/SCDC.Sample.Api/SCDC.Sample.Api.csproj
COPY Shared/ Shared/
COPY Sample/ Sample/
RUN dotnet publish Sample/SCDC.Sample.Api/SCDC.Sample.Api.csproj \
    --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SCDC.Sample.Api.dll"]

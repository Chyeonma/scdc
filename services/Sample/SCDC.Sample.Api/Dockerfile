# syntax=docker/dockerfile:1
# Build context: repository root. Only Sample, Shared.Core and its text policy are copied.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/Shared/SCDC.Shared.Core/SCDC.Shared.Core.csproj", "services/Shared/SCDC.Shared.Core/"]
COPY ["services/Sample/SCDC.Sample.Domain/SCDC.Sample.Domain.csproj", "services/Sample/SCDC.Sample.Domain/"]
COPY ["services/Sample/SCDC.Sample.Application/SCDC.Sample.Application.csproj", "services/Sample/SCDC.Sample.Application/"]
COPY ["services/Sample/SCDC.Sample.Infrastructure/SCDC.Sample.Infrastructure.csproj", "services/Sample/SCDC.Sample.Infrastructure/"]
COPY ["services/Sample/SCDC.Sample.Api/SCDC.Sample.Api.csproj", "services/Sample/SCDC.Sample.Api/"]
COPY docs/fixtures/text-policy.json docs/fixtures/text-policy.json
RUN dotnet restore services/Sample/SCDC.Sample.Api/SCDC.Sample.Api.csproj
COPY services/Shared/ services/Shared/
COPY services/Sample/ services/Sample/
RUN dotnet publish services/Sample/SCDC.Sample.Api/SCDC.Sample.Api.csproj \
    --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SCDC.Sample.Api.dll"]

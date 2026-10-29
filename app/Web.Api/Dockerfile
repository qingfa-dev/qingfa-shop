# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Initialize: copy the build graph only, so a source edit does not invalidate restore.
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props global.json nuget.config ./
COPY src/Core/Core.csproj src/Core/packages.lock.json src/Core/
COPY src/Application/Application.csproj src/Application/packages.lock.json src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/packages.lock.json src/Infrastructure/
COPY src/ServiceDefaults/ServiceDefaults.csproj src/ServiceDefaults/packages.lock.json src/ServiceDefaults/
COPY app/Web.Api/Web.Api.csproj app/Web.Api/packages.lock.json app/Web.Api/

# Detail: restore writes into the image layer, not a cache mount. BuildKit does not
# export cache mounts to the GitHub Actions cache, so a cached restore step would
# leave the publish step without its packages and fail with NETSDK1064.
RUN dotnet restore app/Web.Api/Web.Api.csproj --locked-mode

COPY . .
RUN dotnet publish app/Web.Api/Web.Api.csproj -c Release -o /app \
    --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=6 \
  CMD wget -q -O /dev/null http://localhost:8080/alive || exit 1
ENTRYPOINT ["dotnet", "Web.Api.dll"]

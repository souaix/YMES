FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY YMes.Contracts/MES.Contracts.csproj YMes.Contracts/
COPY YMes.Api/YMes.Api.csproj YMes.Api/
RUN dotnet restore YMes.Api/YMes.Api.csproj

COPY YMes.Contracts/ YMes.Contracts/
COPY YMes.Api/ YMes.Api/
RUN dotnet publish YMes.Api/YMes.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080

COPY --from=build /app/publish ./
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data/dpkeys \
    && mkdir -p /data/qc112 \
    && chown "$APP_UID":"$APP_UID" /data/dpkeys /data/qc112
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "YMes.Api.dll"]

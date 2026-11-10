FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /source

COPY YMes.Contracts/MES.Contracts.csproj YMes.Contracts/
COPY YMes.Web/MES.Web.csproj YMes.Web/
RUN dotnet restore YMes.Web/MES.Web.csproj

COPY YMes.Contracts/ YMes.Contracts/
COPY YMes.Web/ YMes.Web/
RUN dotnet publish YMes.Web/MES.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://127.0.0.1:18090
COPY --from=build /app/publish ./
RUN mkdir -p /data/dpkeys && chown "$APP_UID":"$APP_UID" /data/dpkeys
USER $APP_UID
EXPOSE 18090
ENTRYPOINT ["dotnet", "MES.Web.dll"]

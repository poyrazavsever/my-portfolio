# Çok aşamalı build: SDK imajında derle, yalnızca çalışma zamanı imajını yayınla (~10 kat daha küçük).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce yalnızca proje dosyasını kopyalayıp restore et: kod değişse bile paketler önbellekten gelir.
COPY Portfolio/Portfolio.csproj Portfolio/
RUN dotnet restore Portfolio/Portfolio.csproj

COPY Portfolio/ Portfolio/
RUN dotnet publish Portfolio/Portfolio.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataProtection__KeysPath=/app/keys \
    TZ=Europe/Istanbul

COPY --from=build /app/publish .

# Kalıcı (volume) klasörler: yüklenen görseller, CV ve Data Protection anahtarları.
# Uygulama root değil, imajın hazır "app" kullanıcısıyla ($APP_UID) çalışır.
RUN mkdir -p /app/wwwroot/uploads /app/App_Data /app/keys \
 && chown -R $APP_UID:$APP_UID /app/wwwroot/uploads /app/App_Data /app/keys
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Portfolio.dll"]

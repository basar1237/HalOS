# HalOS Notification (Bildirim) API — çok aşamalı .NET 8 imajı (docs/04, docs/06 S2.2).
# Canlı SignalR dashboard: SaleCompleted'i tüketip patronun ekranına gerçek zamanlı özet yayınlar.
# Bağlam (context) repo köküdür; böylece building-blocks/* proje referansları restore edilebilir.
#   docker compose --profile apps up -d notification
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Yalnız gerekli proje ağacını kopyalayıp restore et (katman önbelleği için önce csproj'lar).
COPY global.json nuget.config Directory.Build.props ./
COPY building-blocks/ building-blocks/
COPY services/Notification/ services/Notification/

RUN dotnet restore "services/Notification/HalOS.Notification.Api/HalOS.Notification.Api.csproj"
RUN dotnet publish "services/Notification/HalOS.Notification.Api/HalOS.Notification.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HalOS.Notification.Api.dll"]

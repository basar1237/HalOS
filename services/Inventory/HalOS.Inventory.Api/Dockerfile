# HalOS Inventory (Stok & Depo) API — çok aşamalı .NET 8 imajı (docs/04, deploy/README).
# Bağlam (context) repo köküdür; böylece building-blocks/* proje referansları restore edilebilir.
#   docker compose --profile apps up -d inventory
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Yalnız gerekli proje ağacını kopyalayıp restore et (katman önbelleği için önce csproj'lar).
COPY global.json nuget.config Directory.Build.props ./
COPY building-blocks/ building-blocks/
COPY services/Inventory/ services/Inventory/

RUN dotnet restore "services/Inventory/HalOS.Inventory.Api/HalOS.Inventory.Api.csproj"
RUN dotnet publish "services/Inventory/HalOS.Inventory.Api/HalOS.Inventory.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HalOS.Inventory.Api.dll"]

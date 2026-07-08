# HalOS API Gateway (BFF) — çok aşamalı .NET 8 imajı (docs/04 §3). YARP ters-vekil; DbContext yok.
# Bağlam (context) repo köküdür (Directory.Build.props/global.json için).
#   docker compose --profile apps up -d gateway
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json nuget.config Directory.Build.props ./
COPY services/Gateway/ services/Gateway/

RUN dotnet restore "services/Gateway/HalOS.Gateway/HalOS.Gateway.csproj"
RUN dotnet publish "services/Gateway/HalOS.Gateway/HalOS.Gateway.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HalOS.Gateway.dll"]

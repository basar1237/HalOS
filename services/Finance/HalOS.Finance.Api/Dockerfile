# HalOS Finance (Cari & Finans) API — çok aşamalı .NET 8 imajı (docs/04, deploy/README).
# Bağlam (context) repo köküdür; böylece building-blocks/* proje referansları restore edilebilir.
#   docker compose --profile apps up -d finance
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Yalnız gerekli proje ağacını kopyalayıp restore et (katman önbelleği için önce csproj'lar).
COPY global.json nuget.config Directory.Build.props ./
COPY building-blocks/ building-blocks/
COPY services/Finance/ services/Finance/

RUN dotnet restore "services/Finance/HalOS.Finance.Api/HalOS.Finance.Api.csproj"
RUN dotnet publish "services/Finance/HalOS.Finance.Api/HalOS.Finance.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "HalOS.Finance.Api.dll"]

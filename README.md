# HalOS — Sebze Meyve Hal ERP

HalOS, sebze-meyve halleri (komisyoncu/tüccar) için çok kiracılı (multi-tenant),
event-driven bir ERP platformudur. Çekirdek servisler C# / .NET 8 üzerinde
Clean Architecture + DDD + CQRS ile geliştirilir; AI işleri ayrı bir Python/FastAPI
servisinde yaşar. Mimari kararlar için bkz. `docs/04-Sistem-Mimarisi-ve-ADR.md`,
geliştirme kuralları için `docs/07-Claude-Code-Gelistirme-Kurallari.md`.

## Depo Yapısı (monorepo)

```
HalOS/
├── HalOS.sln                       # Çözüm dosyası
├── global.json                     # .NET 8 SDK pin
├── Directory.Build.props           # Ortak build ayarları (nullable, langversion vb.)
├── building-blocks/
│   └── HalOS.BuildingBlocks/       # Paylaşılan çekirdek (Domain/Application/Infrastructure)
├── services/                       # Bağlam başına .NET servisi (ileride)
└── docs/                           # Mimari, domain, PRD, yol haritası dokümanları
```

Her servis dört katmanlıdır: `Domain → Application → Infrastructure → Api`
(bağımlılık yönü `Api → Application → Domain`, `Infrastructure → Application/Domain`).
Domain'de dış bağımlılık yoktur.

## Gereksinimler

- .NET 8 SDK (bkz. `global.json`)
- Node 24 (web/mobil için, ileride)
- Docker (Postgres/Redis/RabbitMQ vb. için, ileride)

## Nasıl Çalıştırılır

```bash
# Bağımlılıkları geri yükle
dotnet restore HalOS.sln

# Tüm çözümü derle
dotnet build HalOS.sln

# Testleri çalıştır (test projeleri eklendikçe)
dotnet test HalOS.sln
```

## Paylaşılan Çekirdek — `HalOS.BuildingBlocks`

Tüm servislerin ortak kullandığı yapı taşları:

- **Domain:** `Entity<TId>`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, `Result` / `Result<T>`
- **Application:** `ICommand` / `ICommand<T>`, `IQuery<T>` (MediatR markerları), `ITenantContext`,
  `ValidationBehavior<TRequest,TResponse>` (MediatR + FluentValidation pipeline)
- **Infrastructure:** `TenantDbContextBase` (EF Core; `TenantId` için global query filter),
  `OutboxMessage` + `IOutboxWriter`

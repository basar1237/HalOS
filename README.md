# HalOS — Sebze Meyve Hal ERP

HalOS, sebze-meyve halleri (komisyoncu/tüccar) için çok kiracılı (multi-tenant),
event-driven bir ERP platformudur. Türk hal mevzuatına (HKS, 5957 sayılı Kanun, e-Fatura HAL
senaryosu, e-Müstahsil Makbuzu, komisyon/stopaj/rüsum, künye) göre tasarlanmıştır.

Çekirdek servisler **C# / .NET 8** üzerinde Clean Architecture + DDD + CQRS (MediatR) + EF Core
ile; servisler-arası olaylar **RabbitMQ + MassTransit** (tenant-aware transactional outbox) ile;
AI işleri ayrı bir **Python / FastAPI + Claude** servisinde; web konsolu **Next.js 14 / TypeScript**
ile geliştirilir. Mimari kararlar için `docs/04-Sistem-Mimarisi-ve-ADR.md`, geliştirme kuralları
için `docs/07-Claude-Code-Gelistirme-Kurallari.md`.

## Mimari (özet)

```
İstemciler (Web Konsol · Mobil · Tauri)
        │
        ▼
   API Gateway (BFF, YARP)        ← tek giriş; JWT'yi geçirir, CORS, /hubs proxy
        │
        ├── Identity        (kimlik, JWT/2FA, tenant)
        ├── Party           (müstahsil/alıcı/tüccar cari kartları)
        ├── Sales           (mal geliş, satış, komisyon/kesinti/hakediş)
        ├── Finance         (cari, ödeme, tahsilat, avans, yaşlandırma)
        ├── Inventory       (stok, depo, fire, ürün kataloğu)
        ├── Integration     (e-Fatura, e-MM, HKS bildirim, künye)  [GİB/HKS gateway STUB]
        ├── Search          (Elasticsearch okuma-modeli)
        ├── Notification    (canlı SignalR dashboard — salt tüketici)
        └── AI Gateway      (Python/FastAPI + Claude — "AI muhasebeci", salt-okuma)

Olay omurgası: RabbitMQ (MassTransit) · Veri: PostgreSQL (servis başına DB) + Redis +
Elasticsearch + MinIO · Gözlemlenebilirlik: Serilog + OpenTelemetry + Seq
```

Her .NET servisi dört katmanlıdır: `Domain → Application → Infrastructure → Api`
(bağımlılık yönü `Api → Application → Domain`, `Infrastructure → Application/Domain`).
Domain'de dış bağımlılık yoktur. Multi-tenant izolasyon (BK-8) tenant global query filter ile
her serviste zorlanır.

## Depo Yapısı (monorepo)

```
HalOS/
├── HalOS.sln                       # .NET çözümü (tüm servisler + testler)
├── global.json                     # .NET 8 SDK pin
├── Directory.Build.props           # Ortak build ayarları
├── building-blocks/                # Paylaşılan çekirdek
│   ├── HalOS.BuildingBlocks.Domain / .Application / .Infrastructure
│   └── HalOS.BuildingBlocks.Contracts   # servisler-arası event sözleşmeleri
├── services/                       # Bağlam başına .NET servisi (her biri 4-5 proje)
│   ├── Identity · Party · Sales · Finance · Integration
│   ├── Inventory · Search · Notification
│   └── Gateway                     # API Gateway (BFF, YARP)
├── ai-gateway/                     # Python/FastAPI + Claude (HalOS.sln'de DEĞİL)
├── web/console/                    # Next.js 14 yönetim konsolu
├── deploy/docker-compose.yml       # Tüm altyapı + uygulama servisleri
└── docs/                           # Mimari (04), domain (02), PRD (03), yol haritası (06)
```

## Gereksinimler

- **.NET 8 SDK** (bkz. `global.json`)
- **Node 20+** (web konsolu için)
- **Docker + Docker Compose** (altyapı ve tam yığın için)
- (opsiyonel) **Python 3.12** — AI Gateway'i yerelde çalıştırmak için

## Çalıştırma

### Tüm yığın — Docker (önerilen)

```bash
# Altyapı (Postgres/Redis/RabbitMQ/Elasticsearch/MinIO/Seq)
docker compose -f deploy/docker-compose.yml up -d

# Uygulama servisleri + API Gateway + Web Konsol (apps profili)
docker compose -f deploy/docker-compose.yml --profile apps up -d --build
```

Servisler ayağa kalkınca:

| Bileşen | Yerel URL |
|---|---|
| Web Konsol | http://localhost:3000 |
| API Gateway (BFF) | http://localhost:5000 |
| RabbitMQ yönetim | http://localhost:15672 |
| Seq (loglar) | http://localhost:5341 |

Web konsolu Gateway'e (`NEXT_PUBLIC_API_BASE_URL=http://localhost:5000`) konuşur; istekler
`/api/{servis}` önekiyle ilgili servise yönlenir, canlı dashboard `/hubs/dashboard` üzerinden.

### Yerel geliştirme (Docker'sız servis)

```bash
# Backend
dotnet restore HalOS.sln
dotnet build HalOS.sln
dotnet test HalOS.sln            # 329 test

# Bir servisi çalıştır (örn. Identity, http://localhost:5053)
dotnet run --project services/Identity/HalOS.Identity.Api

# API Gateway (http://localhost:5000) — Development'ta localhost portlarına yönlenir
dotnet run --project services/Gateway/HalOS.Gateway

# Web konsolu (http://localhost:3000)
cd web/console
cp .env.example .env.local        # NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
npm install
npm run dev
```

### Testler

```bash
dotnet test HalOS.sln             # .NET: 329 test (9 servis)
cd web/console && npm test        # Frontend: Vitest birim testleri
cd ai-gateway && pytest           # AI Gateway: pytest
```

## Fazlar (docs/06)

- **Faz 0–1 (MVP):** Kimlik, taraflar, mal geliş, satış+komisyon, cari/finans, e-belge/HKS
  (STUB gateway), stok/fire, raporlar, denetim — **KOD TAMAM**.
- **Faz 2:** Gelişmiş stok, Elasticsearch arama, AI Gateway, canlı SignalR dashboard, ürün
  kataloğu, API Gateway, web konsolu (okuma+yazma) — **KOD TAMAM**.
- **Sıradaki:** mobil (React Native/Expo), masaüstü (Tauri), gerçek GİB/HKS sandbox
  entegrasyonu (dış kimlik gerekir).

## Notlar

- Tüm GİB/HKS/e-Fatura/e-MM gateway çağrıları şu an **STUB**'tır (Faz 1 borcu); sözleşmeler ve
  idempotency/retry mantığı hazırdır, gerçek sandbox entegrasyonu dış kimlik bilgisi gerektirir.
- AI Gateway `ANTHROPIC_API_KEY` yoksa anahtarsız **stub LLM** ile çalışır (gerçek Claude çağrısı yapmaz).
- İş kuralları koda `BK-N` olarak gömülüdür (ör. BK-2 banker's rounding, BK-6 nakit eşiği,
  BK-7 fire negatif olamaz, BK-8 tenant izolasyonu).

# HalOS — Yerel Altyapı (docker-compose)

Bu klasör, HalOS'un yerel geliştirme altyapısını tek komutla ayağa kaldırır
(bkz. `docs/04-Sistem-Mimarisi-ve-ADR.md` §9). Uygulama servisleri (.NET/Python)
varsayılan olarak burada **çalışmaz**; yalnızca bağımlı altyapı bileşenleri çalışır.
Konteynerleştirilmiş uygulama servisleri (örn. Finance) opsiyonel bir profille
(`--profile apps`) devreye alınabilir — bkz. "Uygulama Servisleri (opsiyonel)".

## İçindeki Servisler

| Servis | İmaj | Amaç (ADR) | Host Portu |
|--------|------|------------|------------|
| PostgreSQL | `postgres:16-alpine` | Birincil OLTP (ADR-007) | `5432` |
| Redis | `redis:7-alpine` | Cache / kilit (ADR-007) | `6379` |
| RabbitMQ | `rabbitmq:3.13-management-alpine` | Event bus (ADR-006) | `5672` (AMQP), `15672` (UI) |
| Elasticsearch | `elasticsearch:8.14.3` (single-node) | Arama (ADR-007) | `9200` |
| MinIO | `minio/minio` | Nesne deposu (ADR-007) | `9000` (API), `9001` (Console) |
| Seq | `datalust/seq:2024.3` | Yapısal log — Serilog→Seq (04 §8) | `5341` (UI), `5342` (ingestion) |

Tüm portlar `.env` üzerinden değiştirilebilir (çakışma durumunda).

## Ön Koşullar

- Docker Desktop (Compose v2 dahil) çalışır durumda.
- Elasticsearch için Docker'a en az ~2 GB bellek ayrılmış olması önerilir.

## Hızlı Başlangıç

```bash
# 1) Ortam dosyasını oluştur (parolalar yalnızca yerel geliştirme içindir)
cd deploy
cp .env.example .env      # Windows PowerShell: Copy-Item .env.example .env

# 2) Tüm altyapıyı başlat
docker compose up -d

# 3) Durum ve sağlık kontrolü
docker compose ps

# 4) Logları izle (örn. sadece postgres)
docker compose logs -f postgres
```

Windows'ta (PowerShell) tam yolla:

```powershell
docker compose -f C:/Users/basary/Desktop/HalOS/deploy/docker-compose.yml up -d
```

## YAML Doğrulama (ayağa kaldırmadan)

```bash
docker compose -f C:/Users/basary/Desktop/HalOS/deploy/docker-compose.yml config
```

## Bağlantı Bilgileri (varsayılan `.env` ile)

| Servis | Bağlantı |
|--------|----------|
| PostgreSQL | `Host=localhost;Port=5432;Database=halos;Username=halos;Password=halos_dev_pw` |
| PostgreSQL (Finance) | `Host=localhost;Port=5432;Database=halos_finance;Username=halos;Password=halos_dev_pw` |
| Redis | `localhost:6379`, parola `halos_dev_pw` |
| RabbitMQ | AMQP `localhost:5672` — UI http://localhost:15672 (halos / halos_dev_pw) |
| Elasticsearch | http://localhost:9200 (dev'de güvenlik kapalı) |
| MinIO | API http://localhost:9000 — Console http://localhost:9001 (halos / halos_dev_pw) |
| Seq | UI http://localhost:5341 — ingestion http://localhost:5342 |

> **Not (Seq portları):** Seq container içinde UI'ı `80`, ingestion'ı `5341`'de
> dinler. Burada UI host'ta `5341`, ingestion host'ta `5342`'ye eşlenir. Serilog
> yapılandırmasında Seq ingestion adresi olarak `http://localhost:5342` kullanın.

## Servis Başına Veritabanı

Her çekirdek servis kendi veritabanına sahiptir (ADR-006). `postgres-init/` altındaki
betik ilk açılışta (boş volume) şu DB'leri oluşturur: `halos_identity`, `halos_party`,
`halos_sales`, `halos_finance`. Var olan bir volume üzerinde DB eklemek için:

```bash
docker compose exec postgres psql -U halos -c "CREATE DATABASE halos_finance OWNER halos;"
```

## Uygulama Servisleri (opsiyonel)

Uygulama servisleri normalde yerelde `dotnet run` ile çalıştırılır. Konteyner olarak
çalıştırmak isterseniz `apps` profili kullanılır (ilgili servis altında bir Dockerfile
gerekir):

```bash
# Finance (Cari & Finans, M6) — SaleCompleted'i RabbitMQ'dan tüketir, halos_finance'e yazar.
docker compose --profile apps up -d finance
```

Finance host portu `.env` içinde `FINANCE_PORT` ile ayarlanır (varsayılan `5065`).

## Durdurma / Temizleme

```bash
docker compose down           # container'ları durdur (veriler korunur)
docker compose down -v        # container + volume (TÜM veri silinir)
```

## Sık Karşılaşılan Sorunlar

- **Elasticsearch başlamıyor / hemen kapanıyor:** Docker'ın belleği yetersiz olabilir
  ya da Linux host'ta `vm.max_map_count` düşük. Windows/Docker Desktop'ta genelde
  sorun olmaz; Linux'ta gerekirse `sudo sysctl -w vm.max_map_count=262144`.
- **Port çakışması:** İlgili portu `.env` içinde değiştirin ve `docker compose up -d` tekrar çalıştırın.
- **RabbitMQ sağlıklı olana kadar bekleyin:** İlk açılış (`start_period`) 30 sn'ye kadar sürebilir.

## Güvenlik Notu

`.env.example` içindeki parolalar **yalnızca yerel geliştirme** içindir. Üretimde
sırlar Vault/Key Vault üzerinden yönetilir (04 §7); `.env` dosyası `.gitignore`
kapsamındadır ve depoya işlenmez.

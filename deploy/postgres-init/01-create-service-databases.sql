-- HalOS — servis başına ayrı veritabanı (ADR-006: her servis kendi şemasına/DB'sine sahip).
-- Bu betik yalnızca ilk açılışta (boş data volume) postgres tarafından çalıştırılır
-- (docker-entrypoint-initdb.d). Varsayılan POSTGRES_DB (halos) zaten oluşturulmuştur;
-- burada çekirdek servislerin DB'leri idempotent biçimde eklenir.
--
-- Bağlantı dizeleri (appsettings): her servis kendi DB'sine bağlanır, örn:
--   Finance : Host=localhost;Port=5432;Database=halos_finance;Username=halos;Password=<pw>
--   Sales   : ...;Database=halos_sales;...
--   Party   : ...;Database=halos_party;...
--   Identity: ...;Database=halos_identity;...

SELECT 'CREATE DATABASE halos_identity OWNER halos'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'halos_identity')\gexec

SELECT 'CREATE DATABASE halos_party OWNER halos'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'halos_party')\gexec

SELECT 'CREATE DATABASE halos_sales OWNER halos'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'halos_sales')\gexec

SELECT 'CREATE DATABASE halos_finance OWNER halos'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'halos_finance')\gexec

SELECT 'CREATE DATABASE halos_integration OWNER halos'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'halos_integration')\gexec

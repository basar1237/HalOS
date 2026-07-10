// HalOS Hal Terminali — Tauri v2 masaüstü kabuğu (ADR-005).
//
// Bu katman KASITEN incedir: tüm offline iş mantığı (outbox sırası, idempotency,
// hakediş hesabı, sync motoru) TypeScript tarafındadır ve orada Vitest ile test edilir.
// Rust yalnızca uygulama kabuğunu ve yerel SQLite'ı (tauri-plugin-sql) sağlar; frontend
// SQL'i doğrudan plugin üzerinden çalıştırır. Böylece doğrulanabilir kod TS'te toplanır.

use tauri_plugin_sql::{Migration, MigrationKind};

/// Yerel SQLite şeması (offline-first). docs/04 §5 ve docs/05'e dayanır.
/// Tüm mali kayıtlar append-only; çakışma nadir (docs/04 §5 çakışma kuralı).
fn migrations() -> Vec<Migration> {
    vec![Migration {
        version: 1,
        description: "offline_core_schema",
        sql: r#"
        -- Bekleyen işlemler kuyruğu (outbox). Yerelde commit → kuyruğa al → online olunca gönder.
        -- operation_id: istemci üretimli UUID → çift senkron güvenli (idempotent, docs/04 §5).
        -- seq: aynı aggregate için monoton artan sıra → per-aggregate sıralı gönderim garantisi.
        CREATE TABLE IF NOT EXISTS outbox (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            operation_id   TEXT    NOT NULL UNIQUE,
            aggregate_type TEXT    NOT NULL,
            aggregate_id   TEXT    NOT NULL,
            seq            INTEGER NOT NULL,
            op_type        TEXT    NOT NULL,
            payload        TEXT    NOT NULL,          -- JSON gövde
            status         TEXT    NOT NULL DEFAULT 'pending', -- pending|sending|synced|failed
            attempts       INTEGER NOT NULL DEFAULT 0,
            last_error     TEXT,
            created_at     TEXT    NOT NULL,
            synced_at      TEXT
        );
        CREATE INDEX IF NOT EXISTS ix_outbox_status ON outbox(status, aggregate_id, seq);

        -- Yerel satış başlığı (offline taslak/tamam). Bulut karşılığı sales servisi.
        CREATE TABLE IF NOT EXISTS local_sale (
            operation_id   TEXT PRIMARY KEY,           -- offline idempotency (docs/05 §sale)
            server_id      TEXT,                        -- sync sonrası buluttan gelen id
            party_id       TEXT NOT NULL,               -- alıcı (Buyer)
            party_name     TEXT NOT NULL,
            sale_term      INTEGER NOT NULL,            -- 1=peşin 2=vadeli
            is_within_market INTEGER NOT NULL DEFAULT 1,-- hal içi/dışı → rüsum (BK-5)
            gross_total    TEXT NOT NULL,               -- ondalık string
            status         TEXT NOT NULL DEFAULT 'completed', -- draft|completed|cancelled
            sync_status    TEXT NOT NULL DEFAULT 'pending',   -- pending|synced|conflict
            created_at     TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS local_sale_line (
            id             INTEGER PRIMARY KEY AUTOINCREMENT,
            sale_op_id     TEXT NOT NULL,
            product_id     TEXT NOT NULL,
            product_name   TEXT NOT NULL,
            quantity       TEXT NOT NULL,
            unit_code      INTEGER NOT NULL,            -- 1=kg 2=adet 3=kasa ...
            unit_price     TEXT NOT NULL,
            line_total     TEXT NOT NULL,
            FOREIGN KEY (sale_op_id) REFERENCES local_sale(operation_id) ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ix_sale_line_sale ON local_sale_line(sale_op_id);

        -- Buluttan çekilen master veri önbelleği (son-yazan-kazanır + versiyon damgası).
        CREATE TABLE IF NOT EXISTS cached_product (
            id           TEXT PRIMARY KEY,
            name         TEXT NOT NULL,
            default_unit INTEGER NOT NULL DEFAULT 1,
            row_version  TEXT,                          -- versiyon damgası (çakışma çözümü)
            updated_at   TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS cached_party (
            id           TEXT PRIMARY KEY,
            name         TEXT NOT NULL,
            party_type   INTEGER NOT NULL,              -- 1=Producer 2=Buyer ...
            row_version  TEXT,
            updated_at   TEXT NOT NULL
        );

        -- Sync durumu: entity başına son çekim damgası + anahtar/değer.
        CREATE TABLE IF NOT EXISTS sync_meta (
            key   TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );
        "#,
        kind: MigrationKind::Up,
    }]
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(
            tauri_plugin_sql::Builder::default()
                .add_migrations("sqlite:halos-terminal.db", migrations())
                .build(),
        )
        .run(tauri::generate_context!())
        .expect("HalOS Hal Terminali başlatılamadı");
}

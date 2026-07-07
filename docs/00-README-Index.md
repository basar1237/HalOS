# HalOS — Sebze Meyve Hal ERP · Çekirdek Doküman Seti

> **Çalışma Adı:** HalOS
> **Vizyon Çerçevesi:** *Tarım Tedarik Zinciri İşletim Sistemi*
> **Durum:** Taslak v0.1 · **Dil:** Türkçe · **Sahibi:** Başar Yıldırım

---

## Bu Doküman Seti Nedir?

Bu klasör, HalOS projesinin **kod yazılmadan önce sabitlenen** ürün ve mimari temelidir.
Amaç, geliştiriciye (insan veya AI) mimariyi, iş kurallarını ve domaini **tahmin ettirmeden**
net bir sözleşme sunmaktır.

Bu bir "500 sayfalık şelale dokümanı" **değildir**. Yüksek kaldıraçlı, yalın bir çekirdek
settir (~90 sayfa). Detay ekran spec'leri, ayrıntılı API sözleşmeleri ve UI tasarım sistemi,
ilgili modül geliştirilirken **just-in-time** yazılacaktır.

---

## Okuma Sırası

| # | Doküman | İçerik | Kime? |
|---|---------|--------|-------|
| 00 | [README / Index](./00-README-Index.md) | Navigasyon (bu dosya) | Herkes |
| 01 | [Vizyon ve Ürün Stratejisi](./01-Vizyon-ve-Urun-Stratejisi.md) | Neden, kime, iş modeli | Kurucu, yatırımcı, ürün |
| 02 | [Domain Model ve Ortak Sözlük](./02-Domain-Model-ve-Ortak-Sozluk.md) | **DDD dili — en kritik** | **Herkes (önce bu)** |
| 03 | [PRD — MVP](./03-PRD-MVP.md) | Modüller, roller, iş kuralları | Ürün, geliştirici, test |
| 04 | [Sistem Mimarisi ve ADR](./04-Sistem-Mimarisi-ve-ADR.md) | Stack, servisler, altyapı | Mimar, geliştirici, DevOps |
| 05 | [Veritabanı Çekirdek Şema](./05-Veritabani-Cekirdek-Sema.md) | Tablolar, ilişkiler, indeks | Backend, veri |
| 06 | [Yol Haritası ve Faz Planı](./06-Yol-Haritasi-ve-Faz-Plani.md) | Fazlar, sprint iskeleti | Proje yönetimi, geliştirici |
| 07 | [Claude Code Geliştirme Kuralları](./07-Claude-Code-Gelistirme-Kurallari.md) | Kod standardı, sınırlar | **Her geliştirici / AI ajan** |

> **İlk kez okuyorsan:** Önce **02 (Domain Model)** → sonra **03 (PRD)** → sonra **04 (Mimari)**.
> Kod yazacaksan **07 (Geliştirme Kuralları)** zorunlu okumadır.

---

## Teknoloji Özeti (detay → 04)

| Katman | Seçim |
|--------|-------|
| Çekirdek domain | **C# / .NET 8** · Clean Architecture + DDD + CQRS (MediatR) + EF Core |
| AI servisi (ayrı) | **Python / FastAPI + Anthropic Claude** |
| Web konsol | **Next.js / React / TypeScript** |
| Mobil (patron) | **React Native / Expo** |
| Masaüstü (hal terminali, offline) | **Tauri + React + SQLite** |
| Veri | PostgreSQL · Redis · Elasticsearch · SQLite (offline) · MinIO |
| Altyapı | Docker → Kubernetes · RabbitMQ · Hangfire · SignalR · MQTT (IoT) |

---

## Terminoloji Notu

Bu proje **Türk hal mevzuatına** (5957 sayılı Kanun, HKS, GİB e-Belge) tabidir. Doküman
boyunca kullanılan tüm domain terimleri (müstahsil, komisyoncu, künye, hal rüsumu, müstahsil
makbuzu, stopaj vb.) gerçek yasal/sektörel karşılıklarıdır ve **02-Domain-Model**'de
tanımlanmıştır. Yeni terim uydurmak yasaktır — bkz. 07.

---

## Doküman Bakım Kuralı

- Bu dokümanlar **canlı**dır. Bir modül geliştirilirken karar değişirse **önce doküman
  güncellenir, sonra kod yazılır** — tersi değil.
- Mimari bir kararı değiştirmek isteyen, **04**'e yeni bir **ADR** (Architecture Decision
  Record) ekler; eski kararı silmez, "Superseded" olarak işaretler.
- Her dokümanın başında durum (`Taslak` / `Onaylı` / `Superseded`) ve versiyon bulunur.

---

## Versiyon / Durum Tablosu

| Doküman | Versiyon | Durum | Son Güncelleme |
|---------|----------|-------|----------------|
| 00 Index | v0.1 | Taslak | 2026-07-07 |
| 01 Vizyon | v0.1 | Taslak | 2026-07-07 |
| 02 Domain | v0.1 | Taslak | 2026-07-07 |
| 03 PRD | v0.1 | Taslak | 2026-07-07 |
| 04 Mimari | v0.1 | Taslak | 2026-07-07 |
| 05 Veritabanı | v0.1 | Taslak | 2026-07-07 |
| 06 Yol Haritası | v0.1 | Taslak | 2026-07-07 |
| 07 Claude Kuralları | v0.1 | Taslak | 2026-07-07 |

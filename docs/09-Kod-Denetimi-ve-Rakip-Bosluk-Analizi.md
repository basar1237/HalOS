# 09 — Kod Denetimi ve Rakip Boşluk Analizi

> **Durum:** Taslak v0.1 · **Dil:** Türkçe · **Tarih:** 2026-07-11
> **Amaç:** Ticari hal programlarıyla (Atlas Hal, OnurHal) kıyaslandığında HalOS'un
> kod tabanında neyin DOĞRU, neyin EKSİK, neyin YANLIŞ/riskli olduğunu kanıta dayalı
> tespit etmek ve öncelik sırası çıkarmak. Bulgular dosya:satır referanslıdır.

> ⚠️ **Not:** Bu bir anlık gözlemdir; kod değiştikçe referanslar eskiyebilir. "Doğrulanmalı"
> işaretli maddeler denetim sırasında kesinleştirilememiştir, kod okunarak teyit edilmelidir.

---

## 1. Özet Karar

**Mühendislik omurgası A+, ürün olgunluğu C.**

Çekirdek (para matematiği + tenant izolasyonu + veri tutarlılığı + offline senkron) rakiplerin
çoğundan daha temiz kurulmuştur. Ancak **yasal/operasyonel katmanın %50'den fazlası STUB veya
eksik** olduğundan, HalOS şu an **satılabilir bir hal programı değildir**. En kritik borç:
tüm e-Belge (e-Fatura / e-MM / künye) sahte referans üretmektedir — üretilen hiçbir belge
hukuken geçerli değildir.

---

## 2. DOĞRU Yapılanlar (koddan kanıtlı)

| # | Konu | Kanıt | Değerlendirme |
|---|------|-------|---------------|
| 1 | **Para matematiği** | `services/Sales/HalOS.Sales.Domain/ValueObjects/Money.cs` (decimal + banker's rounding, ToEven); `Domain/Services/SettlementCalculator.cs` (komisyon/stopaj/rüsum atomik); `desktop/src/lib/money.ts` (offline'da aynı algoritma) | Kusursuza yakın. Kuruş yuvarlama hataları yok. BK-1/BK-2 testli. |
| 2 | **Multi-tenant izolasyon (BK-8)** | `building-blocks/.../TenantDbContextBase.cs` (otomatik `HasQueryFilter`); `Identity.Tests/.../TenantQueryFilterTests.cs` (çapraz-tenant negatif test) | Sistematik ve testli. Sızıntı riski düşük. |
| 3 | **Event-driven + transactional outbox** | `building-blocks/.../Messaging/OutboxDispatcher.cs`; `Sales.Infrastructure/.../SalesDbContext.cs` (SaveChanges → outbox atomik) | At-least-once teslim. Mesaj kaybı riski minimal. |
| 4 | **Clean Architecture** | 10 serviste tutarlı 4 katman (Domain→Application→Infrastructure→Api); domain'de dış bağımlılık yok | Katman ihlali yok. |
| 5 | **Offline-first terminal** | `desktop/src/lib/{outbox,sync,conflict,money}.ts` + Vitest testleri | Idempotency + conflict çözümü sağlam. |
| 6 | **Mimari disiplin** | `docs/04` — 10 ADR (bağlam/karar/gerekçe/sonuç) | Kararlar kayıtlı ve izlenebilir. |

---

## 3. EKSİK Olanlar (rakipte var, kodda YOK)

| # | Modül | Rakip | HalOS durumu | Önem |
|---|-------|-------|--------------|------|
| 1 | Çek/Senet yönetimi (portföy, vade, ciro, karşılıksız) | Atlas, OnurHal | Finance'te yalnız tahsilat/ödeme/avans | 🔴 |
| 2 | Çoklu kasa + virman + rehin/ticari kasa ayrımı | Atlas | Kasa kavramı yok | 🔴 |
| 3 | Kantar/terazi donanım entegrasyonu + otomatik dara | OnurHal | Yok | 🔴 |
| 4 | GİB TCKN/VKN canlı doğrulama | OnurHal | Party'de alan var, canlı doğrulama yok | 🔴 |
| 5 | POS tahsilat (banka POS) | Atlas | Yok | 🟡 |
| 6 | e-Arşiv / e-İrsaliye / e-SMM / e-Defter | Atlas | Yalnız e-Fatura + e-MM tipi (stub) | 🟡 |
| 7 | Genel muhasebe / yevmiye / tek düzen hesap planı | Atlas | Yalnız cari (G/L yok) | 🟡 |
| 8 | Rehin fişi (teminat) | Atlas, OnurHal | Yok | 🟡 |
| 9 | Toplu SMS bildirim (mutabakat/borç hatırlatma) | Atlas | Notification yalnız SignalR | 🟡 |
| 10 | Akıllı ajanda / ödeme-tahsilat hatırlatma | OnurHal | AI insights var, zamanlı ajanda yok | 🟢 |
| 11 | Yıl sonu devir | OnurHal | Yok | 🟢 |

---

## 4. YANLIŞ / Riskli Olanlar

### 4.1 🔴 KRİTİK — Tüm e-Belge STUB (sahte referans)
`services/Integration/.../Gateways/StubEDocumentGateway.cs` e-Fatura ("EFA-..."), e-MM ("EMM-...")
ve HKS 19-haneli künye için **uydurma referans** üretir. GİB/HKS sandbox bağlanana kadar üretilen
hiçbir belge hukuken geçerli değildir. Bu Faz-1 borcu bilinçlidir (sözleşme + retry/outbox hazır),
ancak ürünün özü budur — "gerçek hal programı" olma eşiği buradan geçer.

### 4.2 ⚠️ Erken mikroservis (premature microservices)
Gün-1'de 10 servis + RabbitMQ + ~62 proje. Modüler yapı ve monorepo (tek dağıtım) olumlu; ancak
az kişilik ekip/erken aşama için operasyonel yük ağır (10 port, docker kaynak yükü, servisler-arası
event versiyon uyumu). **Öneri:** kademeli docker profilleri — `minimal` (Identity/Sales/Finance),
`dev` (çekirdek), `full` (10 servis).

### 4.3 ⚠️ Outbox'ta sessiz hata
`OutboxDispatcher` döngüsü exception'ı yutup devam ediyor (retry iyi) ama **telemetri yok** →
"belge hiç gitmedi / kuyruk şişti" durumu geç fark edilir. **Öneri:** OpenTelemetry sayaçları
(pending count, error count, deadletter).

### 4.4 ⚠️ AI Gateway anahtarsız stub
`ANTHROPIC_API_KEY` yoksa gerçek Claude çağrısı yapılmaz; demo ile prod davranış farkı gizli kalır.

### 4.5 Doğrulanmalı (denetimde kesinleşmedi)
- API Gateway (YARP) JWT'yi arka servislere düzgün taşıyor mu; arka servis-servis çağrısında token doğrulaması.
- `CompleteSale` ve mali uçlarda RBAC (docs/03 §3 yetki matrisi) gerçekten uygulanmış mı.
- Npgsql `EnableLegacyTimestampBehavior` switch'inin prod timezone/DB ayarında davranışı.
- API endpoint (HTTP + auth) düzeyinde test kapsamı (domain testleri güçlü, endpoint testleri zayıf görünüyor).

---

## 5. Öncelik Sırası (değer/risk)

1. **GİB/HKS sandbox entegrasyonu** — stub'ı gerçeğe çevir (dış kimlik gerekir; kritik yol, ürünün eşiği).
2. **Çek/Senet + Kasa** — nakit akışının belkemiği (Finance'e temiz eklenir).
3. **Kantar/terazi entegrasyonu** — hal = tartı işi.
4. **Operasyon sertleştirme** — outbox observability + minimal docker profili + endpoint testleri.
5. Sonraki: POS, e-Arşiv/e-İrsaliye/e-SMM, rehin fişi, SMS, yıl sonu devir, genel muhasebe.

---

## 6. Rakip Konumlandırma Notu

Atlas Hal / OnurHal: çek portföyü, POS/terazi, gerçek e-Belge entegrasyonu, muhasebe bağlantısı
**mevcut** ama **eski Windows masaüstü** mimarisinde. HalOS'un yapısal üstünlüğü (bulut-doğuştan,
AI, offline terminal, soğuk zincir, multi-tenant) taklit edilemez; ancak bu üstünlük ancak
**giriş bileti** (yukarıdaki eksikler + gerçek e-Belge) tamamlandıktan sonra satışa döner.
Strateji: önce giriş biletini eşitle, sonra AI'ı yıldız özellik olarak öne çıkar.

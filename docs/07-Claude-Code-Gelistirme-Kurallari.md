# 07 — Claude Code / Geliştirme Kuralları

> **Durum:** Taslak v0.1 · **Dil:** Türkçe
> **Kimin için:** Bu projede kod yazan **her geliştirici ve her AI ajanı** (Claude Code dahil).
> **Bu doküman bağlayıcıdır.** Çelişki halinde: 02 (dil) > 04 (mimari) > bu doküman > kişisel tercih.

---

## 0. TEMEL DİREKTİF (Claude Code / AI ajanları için)

> **1. Bu klasördeki (00–07) dokümanlara %100 uy.**
> **2. Mimari bir kararı KENDİN VERME.** Doküman bir konuda sessizse veya çelişiyorsa: **DUR ve SOR.** Uydurma.
> **3. Önce PLAN yap, onaylat, sonra kod yaz.** Büyük değişikliğe doğrudan başlama.
> **4. Sen geliştiricisin, mimar değilsin.** Mimari değişiklik gerekiyorsa → 04'e ADR öner, onay bekle.
> **5. Yeni terim uydurma.** Tüm isimler 02'deki Ortak Sözlük'ten (İngilizce kod adları bağlayıcı).
> **6. İş kuralı (03 §4 BK-x) değiştirmeye kalkma.** Kurallar testle korunur; test kırılıyorsa önce kuralı doğrula.

Belirsizlik anında varsayım yapıp ilerlemek yerine **soru sormak** her zaman tercih edilir.

---

## 1. Dil ve Teknoloji Sınırları (04'ten)

| Alan | Zorunlu |
|------|---------|
| Çekirdek servisler | C# / .NET 8, Clean Architecture + DDD + CQRS (MediatR) + EF Core + FluentValidation |
| AI servisi | Python / FastAPI, ayrı repo/servis; domain'e AI kodu sızmaz |
| Web | Next.js / React / TypeScript |
| Mobil | React Native / Expo |
| Masaüstü | Tauri + React + SQLite |

AI kodu (LLM çağrısı, prompt, embedding) **asla** .NET domain/application katmanına girmez;
yalnızca AI Gateway (Python) içinde yaşar (04 ADR-002).

---

## 2. Klasör Yapısı (her .NET servisi aynı)

```
services/<ServiceName>/
├── <ServiceName>.Domain/          # Entity, ValueObject, Aggregate, DomainEvent, arayüz. DIŞ BAĞIMLILIK YOK.
├── <ServiceName>.Application/     # Command/Query + Handler (MediatR), Validator (FluentValidation), DTO, port arayüzleri
├── <ServiceName>.Infrastructure/  # EF Core DbContext, Repository, dış servis client, mesajlaşma, cache
├── <ServiceName>.Api/             # Controller/Minimal API, DI kompozisyon, middleware, config
└── <ServiceName>.Tests/           # Unit (Domain/Application) + Integration (Infrastructure/Api)
```

**Bağımlılık yönü (ihlal = red):** `Api → Application → Domain`; `Infrastructure → Application, Domain`.
Domain hiçbir şeye bağımlı değildir (ne EF, ne MediatR attribute, ne dış paket).

Web (Next.js): `app/` (route), `components/`, `features/<modul>/`, `lib/`, `shared/` (tip/bileşen paylaşımı mobil ile).

---

## 3. İsimlendirme (Naming)

| Öğe | Kural | Örnek |
|-----|-------|-------|
| C# sınıf/metot/property | PascalCase | `SaleTransaction`, `CalculateSettlement` |
| C# alan (private) | `_camelCase` | `_repository` |
| Domain kavramı | **02 Ortak Sözlük'ten** (İngilizce kod adı) | `Producer`, `Commission`, `Settlement` |
| Domain event | PascalCase, geçmiş zaman | `SaleCompleted`, `PaymentDue` |
| Command / Query | fiil + Command/Query | `CreateSaleCommand`, `GetCurrentAccountQuery` |
| TS/React | değişken/fonksiyon camelCase, bileşen PascalCase | `useSale`, `SaleForm` |
| DB tablo/kolon | snake_case (05'teki adlar) | `sale_transaction`, `tenant_id` |
| Dosya (TS) | kebab veya PascalCase (bileşen) | `sale-form.tsx`, `SaleForm.tsx` |

Türkçe domain terimi ↔ İngilizce kod adı eşlemesi **02**'dedir; kodda İngilizce kod adı,
kullanıcıya görünen metinde Türkçe.

---

## 4. Para / Hesaplama Kuralı (KRİTİK — BK-2)

- Tüm parasal değerler **`decimal`** (C#) / `NUMERIC(18,2)` (DB). **Asla** `float`/`double`/`real`.
- Oranlar `decimal` / `NUMERIC(7,4)` (örn. `0.0800m`).
- Yuvarlama: her ara adımda değil, **son adımda** kuruşa (`Math.Round(x, 2, MidpointRounding.ToEven)`).
- Hesap sırası sabit: yüzde uygula → topla → yuvarla (03 §4 BK-1 örneğiyle birebir).
- Para hesabı yapan her fonksiyon **birim testli** (aşağıdaki örnek 88 TL senaryosu dahil).
- Komisyon oranı **> %8 olamaz** (validasyon + test).

---

## 5. CQRS / MediatR Konvansiyonu

- Yazma → **Command** (`ICommand`/`IRequest`), her komutun bir Handler'ı.
- Okuma → **Query**; ağır okumalar okuma modeli/ES üzerinden (04, 05 §6).
- Her Command/Query için **FluentValidation** validator; validasyon pipeline behavior ile.
- Yan etki (event yayını) **outbox** ile atomik (04 §10); handler içinde doğrudan HTTP çağrısı yok.
- Cross-service çağrı asenkron event tercih; senkron gerekiyorsa açıkça gerekçelendir.

---

## 6. Multi-Tenant Kuralı (BK-8)

- Her iş entity'sinde `TenantId`; EF Core **global query filter** ile otomatik filtre.
- Tenant claim JWT'den; request context'e taşınır; repository asla filtresiz sorgu yapmaz.
- Testlerde çapraz-tenant erişim **negatif testi** zorunlu.

---

## 7. Test Stratejisi

| Katman | Test tipi | Kapsam |
|--------|-----------|--------|
| Domain | Unit | İş kuralları, değişmezler (BK-1..BK-9) — hızlı, bağımsız |
| Application | Unit | Handler + validator (mock repo/port) |
| Infrastructure/Api | Integration | Gerçek Postgres (Testcontainers), migration, event |
| Uçtan uca | E2E | Kritik akış: mal geliş → satış → e-belge → cari (03 §1) |

- **TDD teşvik edilir**: iş kuralı için önce test.
- e-Belge/HKS gibi dış bağımlılıklar test için **fake/sandbox** ile soyutlanır (port arayüzü).
- PR, test yeşil olmadan merge edilmez (§9). Faz kapısı için 06 §7.

---

## 8. Migration ve Veri

- Şema değişikliği **yalnızca EF Core migration** ile; elle DB düzenleme yok.
- Migration adı anlamlı: `AddSettlementDueDate`. Üretimde migration deploy'da kontrollü uygulanır.
- Geriye dönük uyumluluk: kolon silmeden önce iki aşamalı (ekle→taşı→kaldır).
- Mali tabloya **destructive** işlem yasak (05: append-only, ters kayıt).

---

## 9. Commit / Branch / PR

- **Branch:** `feature/<kısa-ad>`, `fix/<kısa-ad>`, `chore/<kısa-ad>`. Main korumalı; doğrudan push yok.
- **Commit:** Conventional Commits — `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`. Kısa, emir kipi.
- **PR:** küçük ve odaklı; açıklamada "ne + neden + hangi BK/ADR"; test kanıtı; ilgili doküman güncellendi mi.
- **Review:** en az bir onay; iş kuralı/mali değişiklik ekstra dikkat.
- Hiçbir zaman `--no-verify` / hook atlama / imza bypass (kullanıcı açıkça istemedikçe).

---

## 10. Kod Kalitesi

- **SOLID**, küçük fonksiyon, anlamlı isim; erken dönüş; derin iç içe yapıdan kaçın.
- Sihirli sabit yok → adlandırılmış sabit/config (özellikle oranlar → tenant config, BK-1).
- Yorum "ne" değil "neden"i anlatır; çevre kodun yoğunluğuna uy.
- Null güvenliği: nullable reference types açık; sınırlarda doğrula.
- Loglama Serilog (yapısal); hassas veri (TCKN/VKN, sır) loglanmaz.
- Hata yönetimi: domain hataları anlamlı; dış çağrı retry/timeout (Polly); kullanıcıya net mesaj.

---

## 11. AI Servisi Sınır Kuralı (ADR-002)

- AI Gateway ERP verisine **API üzerinden, en az yetkiyle** erişir; DB'ye doğrudan girmez.
- MVP sonrası AI **read-only** başlar; yazma işlemi (sipariş/fatura) **her zaman kullanıcı onayı** ile (06 Faz 3).
- Prompt/model/anahtar AI servisinde; birincil model Anthropic Claude (güncel: bkz. claude-api referansı).
- AI çıktısı asla doğrulanmadan mali kayda dönüşmez (insan onayı + validasyon).

---

## 12. Definition of Done (görev bazında)

Bir görev, şunlar sağlanmadan "bitti" değildir:
- [ ] Kod ilgili BK/ADR'ye uygun; 02 sözlüğü kullanılmış.
- [ ] Unit + (gerekliyse) integration testleri yazılmış ve **yeşil**.
- [ ] Lint/format temiz; build uyarısız.
- [ ] Multi-tenant ve yetki (03 §3) gözetilmiş.
- [ ] Karar değiştiyse ilgili doküman + ADR güncellenmiş.
- [ ] PR açıklaması net; "neden" ve doğrulama kanıtı var.

> **Hatırlatma:** Emin değilsen kod yazma — **sor**. Bu projede en pahalı şey, yanlış mimari
> varsayımla yazılmış çok sayıda kodu 6 ay sonra sökmektir. Doküman bunu önlemek için var.

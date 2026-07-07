# 03 — Ürün Gereksinim Dokümanı (PRD) · MVP

> **Durum:** Taslak v0.1 · **Dil:** Türkçe
> **Önkoşul:** [02-Domain-Model](./02-Domain-Model-ve-Ortak-Sozluk.md) (terimler oradan)

Bu doküman **MVP kapsamını** ve **iş kurallarını** tanımlar. Ayrıntılı ekran/piksel spec'leri
kapsam dışıdır (modül geliştirilirken yazılır). Amaç: geliştiricinin *ne* yapacağını ve
*hangi kurala uyacağını* tereddütsüz bilmesi.

---

## 1. MVP Hedefi (Tanımlı Başarı)

> **Bir hal komisyoncusu, bir iş gününü baştan sona HalOS üzerinde yasal olarak
> yönetebilmelidir:** mal kabul → satış → kesinti hesabı → e-Fatura + HKS bildirim →
> müstahsil makbuzu + cari → alıcıdan tahsilat → gün sonu raporu.

MVP tamamlandığında bir işletme, elle/Excel/eski programa ihtiyaç duymadan çalışabilir.

---

## 2. MVP Modülleri

| # | Modül | Kapsam (MVP) | Bağlam (02) |
|---|-------|--------------|-------------|
| M1 | **Taraflar (Cari Kartlar)** | Müstahsil/alıcı/tüccar kaydı, TCKN/VKN, stopaj profili | Party |
| M2 | **Ürün & Birim** | Ürün kataloğu, ölçü birimleri (kasa/kg/çuval/adet/sandık) | Inventory |
| M3 | **Mal Geliş** | Müstahsilden mal kabul, künye, e-İrsaliye referansı | Sales |
| M4 | **Satış / Kasa** | Alıcıya satış, satır girişi, otomatik kesinti hesabı | Sales |
| M5 | **Kesinti & Hakediş** | Komisyon + stopaj + bağ-kur + rüsum + KDV motoru | Sales |
| M6 | **Cari Hesap** | Müstahsil/alıcı bakiye, ödeme, tahsilat, avans | Finance |
| M7 | **Müstahsil Makbuzu** | e-MM üretimi (kayıt tutmayan müstahsil) | e-Belge |
| M8 | **e-Fatura + HKS** | e-Fatura (HAL senaryosu) + HKS bildirim | e-Belge |
| M9 | **Stok (temel)** | Gelen/satılan/kalan, basit fire kaydı | Inventory |
| M10 | **Raporlar** | Gün sonu, satış özeti, cari yaşlandırma, komisyon geliri | (okuma) |
| M11 | **Kullanıcı & Rol** | Tenant, kullanıcı, rol, izin | Identity |

**AI, IoT, WhatsApp, marketplace, açık artırma otomasyonu MVP dışıdır** (bkz. §7).

---

## 3. Kullanıcı Rolleri ve Yetki Matrisi

Roller (02'deki işletme içi kullanıcılar):

| Rol | Açıklama |
|-----|----------|
| **Patron** | İşletme sahibi; tam görünürlük, mali kararlar |
| **Yönetici** | Operasyonu yönetir, kullanıcı/ayar yönetimi (patron hariç) |
| **Muhasebe** | Cari, ödeme/tahsilat, e-belge, raporlar |
| **Satış / Kasiyer** | Satış kaydı, mal geliş, tahsilat girişi |
| **Depo** | Mal kabul, stok, fire |

### 3.1 Yetki Matrisi (MVP)

| Yetenek | Patron | Yönetici | Muhasebe | Satış/Kasiyer | Depo |
|---------|:------:|:--------:|:--------:|:-------------:|:----:|
| Satış kaydı oluştur | ✅ | ✅ | ➖ | ✅ | ➖ |
| Satış sil/iptal | ✅ | ✅ | ➖ | ⚠️* | ➖ |
| Mal geliş kabul | ✅ | ✅ | ➖ | ✅ | ✅ |
| Kesinti oranı değiştir | ✅ | ✅ | ➖ | ➖ | ➖ |
| Cari görüntüle | ✅ | ✅ | ✅ | kısıtlı** | ➖ |
| Ödeme yap (müstahsile) | ✅ | ✅ | ✅ | ➖ | ➖ |
| Tahsilat gir (alıcıdan) | ✅ | ✅ | ✅ | ✅ | ➖ |
| e-Fatura/e-MM üret | ✅ | ✅ | ✅ | otomatik | ➖ |
| HKS bildirim gönder | ✅ | ✅ | ✅ | otomatik | ➖ |
| Stok/fire kaydı | ✅ | ✅ | ➖ | ➖ | ✅ |
| Raporları görüntüle | ✅ | ✅ | ✅ | kendi*** | kendi*** |
| Kullanıcı/rol yönet | ✅ | ✅ | ➖ | ➖ | ➖ |
| Tenant/lisans ayarı | ✅ | ➖ | ➖ | ➖ | ➖ |

\* Kasiyer yalnızca **kendi** oluşturduğu ve **onaylanmamış** satışı iptal edebilir.
\** Kasiyer yalnızca ilgili satış anında cari bakiyeyi görür, tam ekstreyi görmez.
\*** Kendi işlem raporları; işletme geneli mali rapor değil.

> İzin modeli **RBAC** (rol tabanlı) + tenant izolasyonu; teknik detay 04 ve 05.

---

## 4. Çekirdek İş Kuralları (Bağlayıcı)

> Bu kurallar **test edilebilir** yazılmıştır; her biri için unit/integration testi olmalı (07).

### BK-1 — Kesinti ve Hakediş Hesabı
- Brüt satış = `Σ SaleLine.tutar`.
- Kesintiler brüt üzerinden hesaplanır: Komisyon (≤ %8), Zirai Stopaj (%2), Çiftçi Bağ-Kur
  (%1), Hal Rüsumu (hal içi %1 / hal dışı %2).
- Müstahsile net (Hakediş) = Brüt − (Komisyon + Stopaj + Bağ-Kur + Rüsum).
- Komisyon üzerine ayrıca KDV hesaplanır (komisyoncu geliri).
- Örnek (100 TL, hal içi): 100 − 8 − 2 − 1 − 1 = **88 TL** müstahsile.
- Oranlar tenant + tarih + taraf bazında yapılandırılır; **komisyon %8'i aşamaz**.

### BK-2 — Para ve Yuvarlama
- Tüm parasal alanlar **`decimal`**; hesaplama sırası sabit (önce yüzde, sonra yuvarla).
- Yuvarlama: kuruş (2 hane), `MidpointRounding.ToEven` (banker's rounding) — 07'de sabit.

### BK-3 — Müstahsile Ödeme Süresi
- Normal satışta ödeme **15 iş günü**, vadeli satışta **30 gün** içinde planlanır.
- Sistem her müstahsil için `PaymentDue` tarihini otomatik hesaplar ve yaklaşınca uyarır.

### BK-4 — Zorunlu Belgeler
- Her tamamlanan satış: **e-Fatura (HAL senaryosu)** üretir; alıcıya iletir.
- Müstahsil kayıt tutmuyorsa: **e-Müstahsil Makbuzu (e-MM)** üretir.
- Her satış **HKS'e bildirilir** ve **künye** oluşturulur/eşlenir.
- Belge reddi (`DocumentRejected`) durumunda kullanıcı **uyarılır** ve satış "beklemede" işaretlenir.

### BK-5 — Hal Rüsumu
- Hal içi satış %1, hal dışı %2. Belediyeye **5 iş günü** içinde bildirilir; sorumluluk alıcı + komisyoncu ortak.

### BK-6 — Tahsilat/Ödeme Kanalı
- 7.000 TL üstü tahsilat/ödeme **banka üzerinden** yapılmalı ve belgelenmelidir; nakit girişi bu eşiği aşamaz (uyarı).

### BK-7 — Fire
- Stok fire kaydı miktarı düşer; fire oranı ürün bazlı izlenir; hakediş/fiyatlandırmaya etkisi raporlanır.

### BK-8 — Tenant İzolasyonu
- Hiçbir kullanıcı başka tenant'ın verisine erişemez. Her sorgu tenant filtreli (05, 04).

### BK-9 — Satış İptali/Düzeltme
- Onaylanmış ve belgesi kesilmiş satış silinemez; **iade/düzeltme belgesi** ile ters kayıt atılır (denetim izi korunur).

---

## 5. Ekran Envanteri (MVP — üst düzey)

Ayrıntılı tasarım sonra; MVP'de bulunması gereken ekranlar:

| Ekran | Rol(ler) | Not |
|-------|----------|-----|
| Dashboard | Patron, Yönetici | Günlük satış, tahsil edilecek, ödenecek, uyarılar |
| Satış / Kasa | Kasiyer, Yönetici | Hızlı satır girişi, canlı kesinti/hakediş önizleme |
| Mal Geliş | Depo, Kasiyer | Künye, e-İrsaliye referansı |
| Cari Kartları | Muhasebe, Yönetici | Müstahsil/alıcı listesi + ekstre |
| Cari Detay/Ekstre | Muhasebe | Hareketler, bakiye, ödeme/tahsilat |
| Müstahsil Ödeme | Muhasebe | 15 gün kuralı, ödeme planı |
| Ürün & Birim | Yönetici | Katalog |
| Stok | Depo | Gelen/satılan/kalan, fire |
| e-Belge Merkezi | Muhasebe | e-Fatura/e-MM/HKS durumları, red yönetimi |
| Raporlar | Patron, Muhasebe | Gün sonu, satış, yaşlandırma, komisyon |
| Ayarlar | Patron, Yönetici | Kesinti oranları, kullanıcı/rol, entegrasyon anahtarları |
| Giriş / 2FA | Herkes | Kimlik doğrulama |

---

## 6. Fonksiyonel Olmayan Gereksinimler (MVP)

| Konu | Gereksinim |
|------|------------|
| **Offline** | Satış/kasa ekranı internet olmadan çalışır; bağlantı gelince senkronize olur (04 — Sync Engine). |
| **Performans** | Satış kaydı < 300 ms yerel; cari ekstre < 1 sn. |
| **Güvenlik** | JWT + refresh + 2FA; tenant izolasyonu; en az yetki. |
| **Denetim** | Tüm mali işlemler audit log'lu (kim, ne zaman, ne). |
| **Yerelleştirme** | Arayüz Türkçe; para TL; tarih/saat TR. |
| **Yasal veri saklama** | e-Belge ve mali kayıtlar VUK saklama süresince erişilebilir. |
| **Kullanılabilirlik** | Satış ekranı klavye ağırlıklı, hızlı; dokunmatik uyumlu. |

---

## 7. MVP Dışı (v2+ — bilinçli erteleme)

Aşağıdakiler mimaride (04) **hesaba katılır** ama MVP'de **yapılmaz**:

- **AI Agent'lar** (muhasebeci, fiyat, fire, WhatsApp sipariş, sesli komut, evrak okuma) → `08-AI` (Faz 2-3)
- **Soğuk zincir / IoT** (MQTT, sensör, sıcaklık alarmı) → Faz 3
- **Mobil patron uygulaması** (RN/Expo) → Faz 3
- **Açık artırma otomasyonu** → Faz 3+
- **Marketplace / dış entegrasyon** (Logo, kargo, e-Defter aracıları) → Faz 4
- **Çoklu şube / çoklu hal konsolidasyonu** → Faz 4
- **e-Defter** → Faz 2-4 (yasal takvime göre)

> Erteleme sessiz değildir: her biri 06'daki fazlara bağlanmıştır.

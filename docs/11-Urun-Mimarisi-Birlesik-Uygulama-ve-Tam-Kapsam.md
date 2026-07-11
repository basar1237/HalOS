# 11 — Ürün Mimarisi: Birleşik Uygulama ve Tam Kapsam

> **Durum:** Taslak v0.1 · **Dil:** Türkçe · **Tarih:** 2026-07-12
> **Amaç:** Tüm HalOS ürününü TEK master planda toplamak. Rakip paritesi (OnurHal/Atlas/MacVEG)
> + farklılaştırıcılarımız + birleşik uygulama mimarisi + yapım sırası. **Hepsi yapılacak.**
> Bu doküman "tek gerçek kaynak"tır; ekranlar bu spesifikasyona göre kurulur.

---

## 1. Karar: TEK Masaüstü Uygulaması (iki ayrı program değil)

Hal bilgisayarına **iki program** (yönetim konsolu + satış terminali) kurmak YANLIŞ. Bunun yerine:

- **Tek masaüstü uygulaması** (Tauri + React) — kurulum tek, çift tıkla açılır.
- İçinde **sekmeler (tab)**:
  - **Girişler** — hızlı satış (Excel-tarzı grid), mal geliş, hızlı işlemler. Kasiyer/operatör burada.
  - **Kontroller** — dashboard, raporlar, cari, stok, e-belge, ayarlar. Patron/muhasebe burada.
  - Modül sekmeleri: **Satış · Çek/Senet · Kasa · Cari · Stok · e-Belge · Raporlar**.
- **Tek giriş (login), tek pencere.** Girişler tek yerden, kontroller tek yerden.
- **Patron için mobil uygulama** (Expo/RN) — uzaktan izleme + AI asistan (ayrı, hafif).

### 1.1 Konsolidasyon planı
- Bugünkü `web/console` (Next.js) yönetim ekranları + `desktop` (Tauri) satış terminali →
  **tek Tauri uygulamasında** birleştirilecek. Ortak React bileşen kütüphanesi + ortak API istemcisi.
- Backend AYNI kalır: Gateway + mikroservisler (bulut ya da hal-içi sunucu). Offline: yerel SQLite + senkron.
- Web konsol (tarayıcı) **opsiyonel** olarak korunur (bulut erişimi isteyen için); ama birincil ürün masaüstü.

### 1.2 Dağıtım
- **Masaüstü:** tek `.exe`/installer (Tauri bundle). Otomatik güncelleme.
- **Backend:** hal-içi mini sunucu (Docker) VEYA HalOS bulutu (SaaS). Offline-first çalışır.
- **Mobil:** App Store / Play Store (Expo build).

---

## 2. Uygulama Yapısı (birleşik masaüstü)

```
HalOS Masaüstü (tek pencere, tek login)
├── ÜST BAR: logo · dönem · çevrimiçi/senkron · tema (beyaz/koyu) · kullanıcı · çıkış
├── SEKME: Girişler ▼
│   ├── Hızlı Satış (Excel grid — F-tuşları)
│   ├── Mal Geliş (müstahsilden, künye)
│   └── Hızlı Cari/Kasa işlemleri
├── SEKME: Satış & Komisyon (liste + detay + iptal)
├── SEKME: Cari & Finans (hesap, ekstre, tahsilat/ödeme/avans)
├── SEKME: Çek / Senet (portföy, tahsil/tediye, ciro, karşılıksız)
├── SEKME: Kasa (çoklu kasa, tahsil/tediye, virman)
├── SEKME: Stok & Depo (ürün, stok, fire, kantar defteri)
├── SEKME: e-Belge & HKS (e-Fatura, e-Arşiv, e-İrsaliye, e-MM, künye)
├── SEKME: Raporlar (gün sonu, komisyon, cari yaşlandırma, trend)
├── SEKME: AI (yerel Ollama / Claude — sor, öneri, evrak oku)
└── SEKME: Ayarlar (oranlar, kullanıcılar, yedek, cihaz/kantar)
```

---

## 3. Tam Modül & Ekran Kapsamı (OnurHal paritesi + farkımız)

Durum işaretleri: ✅ var · 🟡 kısmi · ❌ yok (yapılacak).

### 3.1 Üretici (Müstahsil) / Müşteri (Alıcı) — Taraflar
- Cari kart: ad, TCKN/VKN, vergi dairesi, GSM, adres, plaka, defter tutar mı, stopaj profili. ✅
- **GİB'den TCKN/VKN canlı doğrulama.** ❌
- Rol: müstahsil / alıcı / tüccar. "Kendi malı" (tüccar = müstahsil kendisi) senaryosu. ❌

### 3.2 Satış / e-Fatura ekranı (ÇEKİRDEK — OnurHal ekranından spec)
Rakip ekranında olan, bizde hedeflenen alanlar:
- **Başlık:** Firma, Vergi dairesi/no, ☑ Vergi mükellefi, GSM, Posta kutusu, **Plaka**, Tarih,
  Fatura no, **İrsaliye No/Tarihi**, Sipariş no. Üstte **Rehin · Cari Defter · Bakiye** canlı. 🟡→❌
- **Satır gridi:** No · Ambar · Marka · Stok/Ürün · **Kap (kasa) · Daralı (brüt kg) · Dara · Safi (net kg)**
  · Fiyat · KDV · Tutar · Ort KG. → **kantar/dara/net ağırlık iş akışı.** ❌
- **Kesintiler/masraflar:** **Hamaliye** (+KDV) · **Nakliye** (+KDV) · **İ.S. sandık** · **Hal rüsum %**
  · Komisyon · Stopaj · Bağ-Kur · KDV. 🟡 (komisyon/stopaj/rüsum var; hamaliye/nakliye/sandık ❌)
- **Toplamlar:** Brüt · Net Toplam · Mal KDV · Peşin · Rehin · Fatura toplam · Müstahsil net hakediş.
- **F-tuşları:** F1 Satır · F2 Fatura · F4 Rehin · F5 Masraf · F6 Veresiye · F7 Yazdır · F8 Düzenle. 🟡
- **e-Belge oto-seçim:** müşteri mükellefse **e-Fatura**, değilse **e-Arşiv**; müstahsil defter tutmuyorsa **e-MM**.
  Ekranda "MÜŞTERİ E-FATURA SİSTEMİNDE" göstergesi. ❌ (mantık stub)
- Kasa teslim şekli (peşin/rehin/veresiye). ❌

### 3.3 Mal Geliş (Konsinye) + Künye
- Müstahsilden parti kabul, ürün/kap/ağırlık, irsaliye ref. ✅
- **HKS künye** (19 hane) otomatik. 🟡 (stub) → gerçek HKS ❌

### 3.4 Cari & Finans
- Cari hesap, ekstre, tahsilat/ödeme/avans, yaşlandırma. ✅
- Cari bakiye satış anında görünür. ❌

### 3.5 Çek / Senet ❌ (yapılacak — büyük)
- Portföy: alınan/verilen, banka, seri no, vade, tutar, durum (portföyde/tahsil/karşılıksız/ciro).
- İşlemler: tahsile ver, tahsil edildi, karşılıksız, ciro et. Banka/kasa bağlantılı.
- Çek-Senet dekontu, hareket fişleri.

### 3.6 Kasa ❌ (yapılacak — büyük)
- Çoklu kasa (ticari / rehin ayrı), kasa defteri, tahsil/tediye fişi, **virman** (kasalar arası).
- Banka + POS tahsilat bağlantısı.

### 3.7 Stok & Depo + Kantar
- Ürün kataloğu, stok, fire, depo/ambar. ✅
- **Kantar defteri** (tartım kayıtları) + terazi/kantar donanım entegrasyonu. ❌

### 3.8 e-Belge & HKS
- e-Fatura · e-Arşiv · e-İrsaliye · e-SMM · e-MM · künye. 🟡 (e-Fatura+e-MM stub; e-Arşiv/e-İrsaliye ❌)
- **Gerçek GİB/HKS entegrasyonu** dış entegratör kimliği gerektirir (harici bağımlılık).

### 3.9 Raporlar
- Gün sonu, satış özeti, komisyon geliri, cari yaşlandırma, trend. ✅
- Kasa/çek/rüsum raporları. ❌

### 3.10 Genel Muhasebe (düşük öncelik)
- Yevmiye, tek düzen hesap planı, muhasebe fişleri. ❌ (çoğu hal mali müşavire bırakır)

### 3.11 Yardımcı
- Kullanıcı/yetki ✅ · log/audit ✅ · **otomatik yedek** 🟡 · **akıllı ajanda (hatırlatma)** ❌ ·
  **yıl sonu devir** ❌ · uygulama içi mesajlaşma ❌.

---

## 4. Farklılaştırıcılar (rakipte YOK — bizim kozumuz)
- **Bulut + offline-first senkron** (masaüstü ama veri bulutta yedekli, çok cihaz).
- **AI** (yerel Ollama / Claude): doğal dille sor, evrak fotoğrafından mal geliş, proaktif uyarı, sesli.
- **Gerçek mobil patron app.**
- **Soğuk zincir / IoT.**
- **Çok kiracılı SaaS**, modern UI, beyaz/koyu tema.

---

## 5. Mobil Patron Uygulaması (Expo/RN)
- Dashboard (günlük satış, açık cari, bekleyen hakediş, alarm).
- Satış/cari/stok özet (salt-okuma).
- Soğuk zincir alarm.
- **AI asistan** (yazılı + sesli): "Ahmet'e bugün ne sattım", "borcu ne", sipariş taslağı → onay.

---

## 6. Yapım Sırası (roadmap)

**Faz A — Birleşik masaüstü iskelet**
1. Tek Tauri uygulaması + sekme kabuğu (Girişler / Kontroller / modüller), tek login, tema.
2. Mevcut console ekranları + terminal satışı bu kabuğa taşınır (ortak bileşen + API istemcisi).

**Faz B — Satış ekranı paritesi (OnurHal düzeni)**
3. Satış gridine kap/dara/safi (kantar), hamaliye/nakliye/İ.S., vergi bilgisi, cari bakiye, F-tuşları.
4. e-Belge oto-seçim (e-Fatura/e-Arşiv/e-MM) + durum göstergesi (mantık; gerçek GİB sonra).

**Faz C — Eksik modüller (nakit akışı)**
5. **Çek/Senet** (Finance'e aggregate + UI). 6. **Kasa** (çoklu kasa + virman + POS).

**Faz D — Operasyon & yasal**
7. Kantar donanım entegrasyonu. 8. e-Arşiv/e-İrsaliye. 9. GİB/HKS gerçek entegrasyon (dış kimlik).
10. GİB TCKN/VKN doğrulama. 11. Akıllı ajanda, yıl sonu devir.

**Faz E — Mobil & AI**
12. Mobil patron app (izleme + AI asistan, sesli).

---

## 7. Notlar / Bağımlılıklar
- **Gerçek e-Fatura/HKS:** GİB entegratör kimliği/sözleşmesi gerekir (harici; bizde yok).
- **Kantar donanımı:** seri/USB terazi sürücüsü (marka bazlı: CAS, Baster vb.).
- Bkz. [09-Kod-Denetimi], [10-Rakip-Sistem-Analizi].

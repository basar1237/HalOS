# 10 — Rakip Sistem Analizi (OnurHal · Atlas Hal · MacVEG · Sa-SelSoft)

> **Durum:** Taslak v0.1 · **Dil:** Türkçe · **Tarih:** 2026-07-11
> **Kaynak:** Rakiplerin halka açık ürün sayfaları, özellik listeleri, sektör normları.
> Teknoloji altyapısı **çıkarımdır** (iç kod/DB halka açık değil; sızma YOK).

---

## 1. Teknoloji Altyapısı (çıkarım, yüksek güven)

Dört rakip de aynı kalıpta:

- **Windows masaüstü** uygulaması — muhtemelen **C#/.NET WinForms** + **DevExpress** grid bileşenleri (Türk ticari yazılımın standardı).
- **Veritabanı:** Microsoft **SQL Server** (çoğu yerel; bazıları hal-içi LAN'da tek sunucu, birkaç kasa aynı DB'ye bağlanır).
- **Kurulum modeli:** her bilgisayara `.exe`/setup ile kurulur; **peşin lisans + yıllık bakım** (SaaS değil).
- "Tüm Windows sürümleriyle uyumlu" (OnurHal), yani platforma kilitli.

## 2. Arayüz & Çalışma Sistemi

- **Yoğun, klavyeyle sürülen grid ekranlar** (Excel benzeri). F-tuşları ve kısayollarla hızlı veri girişi; fare opsiyonel.
- Tipik akış:
  1. **Mal geliş** (müstahsilden) → **künye** al (HKS otomatik).
  2. **Satış**: grid'e satır satır (ürün/miktar/birim/fiyat) — Tab/Enter ile hızlı.
  3. Satışta **otomatik satış künyesi** + HKS bildirimi + **e-belge** (e-Fatura/e-MM).
  4. **Cari / kasa / çek** otomatik işlenir.
- **Otomatik dara, rehin, ambar, yıl sonu devir** (OnurHal).
- **GİB'den TCKN/VKN doğrulama** (OnurHal).
- Görsel olarak **2005–2010 Windows** hissi — fonksiyonel ama modası geçmiş.

## 3. Tam Modül Seti (MacVEG'den net doğrulandı)

Rakiplerin "tam paket"i:

> Cari Hesap · Stok · Fatura · **Çek-Senet** · **Banka** · **Rehin** · **Kira/Hammaliye** ·
> **Kantar Defteri** · **Kasa Defteri** · Müstahsil Faturaları · Künye · Raporlar · Genel Muhasebe

## 4. Zayıf / Açık Noktaları (bizim fırsatımız)

| Açık | Neden zayıf |
|------|-------------|
| Bulut yok | Masaüstüne kilitli; sadece o bilgisayardan erişim |
| Veri tek makinede | Bilgisayar bozulur/çalınırsa felaket; harici diske bağımlı yedek |
| Mobil zayıf/yok | Patron sahadan göremez |
| Gerçek offline+bulut senkron yok | Ağ kopunca çok-kasa tutarlılığı zor |
| AI yok | Kayıt tutar; analiz/öneri/otomasyon vermez |
| Çok şube zor | Her kuruluma ayrı lisans/kurulum |
| Modern UI/tablet yok | Genç kuşağa ve dokunmatiğe uygun değil |
| SaaS/şeffaf fiyat yok | Yüksek peşin lisans + bakım; aylık model yok |

## 5. HalOS Konumlandırması

- **Kopyalanacak (giriş bileti):** klavye hızı + künye/e-belge otomasyonu + tanıdık grid akışı.
  Bunu Hızlı Satış gridi (web konsol + masaüstü terminal) ile karşıladık.
- **Farkımız (taklit edilemez):** bulut-doğuştan + gerçek mobil + offline-first senkron +
  soğuk zincir/IoT + **AI (yerel Ollama veya Claude)** + çok-kiracılı + modern UI.
- **Eşitlenecek eksikler (rakipte var, bizde yok):** Çek-Senet, Banka, **Kasa Defteri**,
  Rehin, Kantar Defteri, Kira/Hammaliye, Genel Muhasebe. Öncelik: **Çek-Senet + Kasa**
  (nakit akışının belkemiği). Bkz. [09-Kod-Denetimi-ve-Rakip-Bosluk-Analizi.md].

## 6. Strateji Özeti

> Rakip "hal defterini bilgisayara taşımış" (kilitli masaüstü). HalOS "işi yöneten AI'lı bulut
> işletim sistemi". Önce giriş biletini (klavye hızı + eksik modüller + gerçek e-belge) eşitle,
> sonra AI + bulut + offline + mobil'i **yıldız özellik** olarak öne çıkar.

# HalOS Patron — Mobil (Expo / React Native)

Sebze-meyve hal patronu için **izleme** uygulaması (ADR-004: React Native / Expo). Web konsoluyla
**aynı API Gateway**'e konuşur; canlı özet, satışlar ve cari bakiyeleri gösterir.

## Ekranlar
- **Giriş** — gerçek Identity (`/api/identity/auth/login`), 2FA destekli; token Secure Store'da.
- **Panel** — günlük satış (net), bekleyen hakediş, açık cari, bugünkü mal geliş, bekleyen e-belge.
- **Satışlar** — son satışlar (tutar/tarih/durum).
- **Cari** — cari hesap bakiyeleri.

## Çalıştırma
```bash
cd mobile
npm install
# API Gateway adresi (gerçek cihazda host LAN IP'si; emülatörde localhost/10.0.2.2):
export EXPO_PUBLIC_API_BASE_URL=http://192.168.x.x:5000
npm start           # Expo Dev — QR ile Expo Go, veya i/a ile emülatör
```

> NOT: Gerçek cihaz `localhost`'a ulaşamaz → `EXPO_PUBLIC_API_BASE_URL` host makinenin LAN IP'si
> olmalıdır. Android emülatöründe host için `http://10.0.2.2:5000` kullanılır.

## Doğrulama
```bash
npm run type-check   # tsc --noEmit
npm test             # Vitest — saf mantık (biçimlendirme) birim testleri
```

## Yapı
```
mobile/
├── app/                    # expo-router (dosya tabanlı yönlendirme)
│   ├── _layout.tsx         # AuthProvider + oturum yönlendirmesi
│   ├── login.tsx
│   └── (tabs)/             # Panel / Satışlar / Cari
└── src/
    ├── lib/                # api (Gateway), auth, token (SecureStore), useQuery, format
    └── shared/types.ts     # backend sözleşmeleriyle aynı tipler
```
Kimlik/tenant sunucuda çözülür (BK-8); istemci yalnız token taşır.

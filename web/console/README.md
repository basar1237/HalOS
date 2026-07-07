# HalOS — Yönetim Konsolu (Web)

Sebze Meyve Hal ERP yönetim konsolu. **Next.js (App Router) + React + TypeScript** (docs/04 ADR-003).

## Yapı (docs/07 §2)

```
src/
├── app/            # Next.js App Router rotaları (layout, login, dashboard)
├── components/     # Paylaşılan UI (sidebar, topbar, providers)
├── features/       # Modül bazlı (auth, tenant)
├── lib/            # API client, token saklama
└── shared/         # Tipler (ileride mobil ile paylaşım)
```

## Çalıştırma

```bash
npm install
npm run dev      # geliştirme
npm run build    # üretim derlemesi
```

## Bu fazdaki kapsam (iskelet)

- Kabuk yerleşimi: kenar menü + üst bar.
- Giriş (login) sayfası — form iskeleti.
- Kontrol paneli — boş kartlar.
- `AuthProvider` + korumalı route (stub), `TenantProvider` (multi-tenant, BK-8).
- API client: fetch sarmalayıcı, JWT `Authorization` başlığı yeri (ADR-009).

Kimlik doğrulama ve veri akışları gerçek Identity/API Gateway servisleriyle ileriki
fazda bağlanacaktır (docs/06). `NEXT_PUBLIC_API_BASE_URL` için `.env.example`'a bakın.

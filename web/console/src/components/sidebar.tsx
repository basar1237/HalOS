'use client';

// Kenar menü — ana modüller (docs/02 bounded context'lerine karşılık gelir).
// Metinler Türkçe; rotalar iskelet (çoğu ileriki fazda gelecek).

import Link from 'next/link';
import { usePathname } from 'next/navigation';

interface NavItem {
  label: string;
  href: string;
}

// docs/02 §2 bağlamlarından türetilmiş menü. Şu an yalnızca dashboard aktif.
const NAV_ITEMS: NavItem[] = [
  { label: 'Kontrol Paneli', href: '/dashboard' },
  { label: 'Satış & Komisyon', href: '/dashboard/satis' },
  { label: 'Cari & Finans', href: '/dashboard/finans' },
  { label: 'Taraflar', href: '/dashboard/taraflar' },
  { label: 'Stok & Depo', href: '/dashboard/stok' },
  { label: 'e-Belge & HKS', href: '/dashboard/e-belge' },
  { label: 'Soğuk Zincir', href: '/dashboard/soguk-zincir' },
  { label: 'AI Önerileri', href: '/dashboard/ai-oneriler' },
  { label: 'Bildirimler', href: '/dashboard/bildirimler' },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="sidebar">
      <div className="sidebar__brand">HalOS</div>
      <nav className="sidebar__nav">
        {NAV_ITEMS.map((item) => {
          const isActive =
            item.href === '/dashboard'
              ? pathname === '/dashboard'
              : pathname.startsWith(item.href);
          return (
            <Link
              key={item.href}
              href={item.href}
              className={
                isActive ? 'sidebar__link sidebar__link--active' : 'sidebar__link'
              }
            >
              {item.label}
            </Link>
          );
        })}
      </nav>
    </aside>
  );
}

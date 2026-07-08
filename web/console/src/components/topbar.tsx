'use client';

// Üst bar — aktif tenant göstergesi, kullanıcı adı, çıkış.

import { useAuth } from '@/features/auth/auth-context';
import { useTenant } from '@/features/tenant/tenant-context';

/** Tenant görünen adı: ad varsa ad, yoksa kısa Id (isim okuma modeli sonraki fazda). */
function tenantLabel(name: string, id: string): string {
  if (name) return name;
  return `İşletme #${id.slice(0, 8)}`;
}

export function Topbar() {
  const { user, logout } = useAuth();
  const { activeTenant } = useTenant();

  return (
    <header className="topbar">
      <div className="topbar__tenant">
        {activeTenant
          ? tenantLabel(activeTenant.name, activeTenant.id)
          : 'İşletme seçilmedi'}
      </div>
      <div className="topbar__right">
        <span className="topbar__user">
          {user ? user.fullName : 'Misafir'}
        </span>
        <button type="button" className="topbar__logout" onClick={logout}>
          Çıkış
        </button>
      </div>
    </header>
  );
}

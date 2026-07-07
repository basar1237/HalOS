'use client';

// Üst bar — aktif tenant göstergesi, kullanıcı adı, çıkış.

import { useAuth } from '@/features/auth/auth-context';
import { useTenant } from '@/features/tenant/tenant-context';

export function Topbar() {
  const { user, logout } = useAuth();
  const { activeTenant } = useTenant();

  return (
    <header className="topbar">
      <div className="topbar__tenant">
        {activeTenant ? activeTenant.name : 'İşletme seçilmedi'}
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

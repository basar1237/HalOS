'use client';

// TenantContext — multi-tenant (docs/07 §6, BK-8, ADR-008).
// Aktif tenant esas olarak JWT claim'inden çözülür; UI aktif tenant'ı burada tutar.

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';

import { useAuth } from '@/features/auth/auth-context';
import type { Tenant } from '@/shared/types';

interface TenantContextValue {
  activeTenant: Tenant | null;
  setActiveTenant: (tenant: Tenant | null) => void;
}

const TenantContext = createContext<TenantContextValue | undefined>(undefined);

export function TenantProvider({ children }: { children: ReactNode }) {
  const { tenantId } = useAuth();
  const [activeTenant, setActiveTenant] = useState<Tenant | null>(null);

  // Aktif tenant oturumdan (JWT/login yanıtı) türer (BK-8). İsim henüz Identity'de okuma
  // modeli olmadığından boş bırakılır (GetById iskelet); UI kısa Id'ye düşer. Kullanıcı elle
  // tenant seçtiyse (setActiveTenant) ona dokunma.
  useEffect(() => {
    setActiveTenant((current) => {
      if (!tenantId) return null;
      if (current?.id === tenantId) return current;
      return { id: tenantId, name: '' };
    });
  }, [tenantId]);

  const value = useMemo<TenantContextValue>(
    () => ({ activeTenant, setActiveTenant }),
    [activeTenant],
  );

  return (
    <TenantContext.Provider value={value}>{children}</TenantContext.Provider>
  );
}

export function useTenant(): TenantContextValue {
  const context = useContext(TenantContext);
  if (context === undefined) {
    throw new Error('useTenant, TenantProvider içinde kullanılmalıdır.');
  }
  return context;
}

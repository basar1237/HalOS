'use client';

// Global istemci sağlayıcıları — Auth + Tenant context'lerini kökte kurar.

import type { ReactNode } from 'react';

import { AuthProvider } from '@/features/auth/auth-context';
import { TenantProvider } from '@/features/tenant/tenant-context';

export function Providers({ children }: { children: ReactNode }) {
  return (
    <AuthProvider>
      <TenantProvider>{children}</TenantProvider>
    </AuthProvider>
  );
}

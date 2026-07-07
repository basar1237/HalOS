'use client';

// Korumalı route mantığı (stub) — oturum yoksa giriş sayfasına yönlendirir.
// ADR-009 uyumlu; gerçek yetki (RBAC, docs/03 §3) ileriki fazda eklenecek.

import { useRouter } from 'next/navigation';
import { useEffect, type ReactNode } from 'react';

import { useAuth } from '@/features/auth/auth-context';

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.replace('/login');
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return <div className="page-state">Yükleniyor…</div>;
  }

  if (!isAuthenticated) {
    // Yönlendirme efekt içinde; bu arada boş içerik gösterilir.
    return null;
  }

  return <>{children}</>;
}

// Kontrol paneli yerleşimi — kenar menü + üst bar; korumalı route ile sarmalı.

import { Sidebar } from '@/components/sidebar';
import { Topbar } from '@/components/topbar';
import { ProtectedRoute } from '@/features/auth/protected-route';

export default function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <ProtectedRoute>
      <div className="app-shell">
        <Sidebar />
        <div className="app-main">
          <Topbar />
          <main className="app-content">{children}</main>
        </div>
      </div>
    </ProtectedRoute>
  );
}

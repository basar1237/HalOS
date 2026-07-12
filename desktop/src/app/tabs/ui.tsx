// Sekmeler arası ortak UI primitive'leri — KPI kartı + grid. Tekrar eden inline stiller
// ve yerel Kpi tanımları burada toplandı (CSS sınıfları: .kpi, .kpi-grid — styles.css).
import type { ReactNode } from 'react';

export function KpiGrid({ children }: { children: ReactNode }) {
  return <div className="kpi-grid">{children}</div>;
}

export function Kpi({ label, value, sub, danger }: { label: string; value: ReactNode; sub?: ReactNode; danger?: boolean }) {
  return (
    <div className={danger ? 'kpi kpi--danger' : 'kpi'}>
      <div className="kpi__label">{label}</div>
      <div className="kpi__value">{value}</div>
      {sub ? <div className="kpi__sub">{sub}</div> : null}
    </div>
  );
}

import { useState } from 'react';
import { NewSale } from './NewSale';
import { SalesList } from './SalesList';
import { SyncBar } from './SyncBar';
import { useTerminal } from './useTerminal';
import { getTheme, setTheme, type Theme } from '../lib/theme';
import { Dashboard } from './tabs/Dashboard';
import { Cari } from './tabs/Cari';
import { Stok } from './tabs/Stok';
import { EBelge } from './tabs/EBelge';
import { Raporlar } from './tabs/Raporlar';
import { Ai } from './tabs/Ai';
import { Cek } from './tabs/Cek';
import { Kasa } from './tabs/Kasa';

interface Props {
  userName?: string;
  onLogout: () => void;
}

const TABS = [
  { key: 'giris', label: 'Girişler', icon: '🧾' },
  { key: 'panel', label: 'Kontrol Paneli', icon: '📊' },
  { key: 'satis', label: 'Satışlar', icon: '📄' },
  { key: 'cari', label: 'Cari & Finans', icon: '💳' },
  { key: 'stok', label: 'Stok & Depo', icon: '📦' },
  { key: 'ebelge', label: 'e-Belge & HKS', icon: '🏷️' },
  { key: 'rapor', label: 'Raporlar', icon: '📈' },
  { key: 'cek', label: 'Çek / Senet', icon: '💠' },
  { key: 'kasa', label: 'Kasa', icon: '💰' },
  { key: 'ai', label: 'AI Muhasebeci', icon: '🤖' },
] as const;

type TabKey = (typeof TABS)[number]['key'];

export function Terminal({ userName, onLogout }: Props) {
  const t = useTerminal();
  const [theme, setThemeState] = useState<Theme>(getTheme());
  const [tab, setTab] = useState<TabKey>('giris');

  function toggleTheme() {
    const next: Theme = theme === 'light' ? 'dark' : 'light';
    setTheme(next);
    setThemeState(next);
  }

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          Halos<span style={{ color: 'var(--accent)' }}>ERP</span>{' '}
          <small>Hal Yönetim Sistemi · {userName ?? 'kullanıcı'}</small>
        </div>
        <div className="syncbar">
          <SyncBar online={t.online} syncing={t.syncing} pending={t.pending} lastSync={t.lastSync} onSync={t.sync} />
          <button className="icon-btn" onClick={toggleTheme} title={theme === 'light' ? 'Koyu tema' : 'Beyaz tema'}>
            {theme === 'light' ? '🌙' : '☀️'}
          </button>
          <button className="ghost" onClick={onLogout}>Çıkış</button>
        </div>
      </header>

      <div className="shell">
        <nav className="sidenav">
          {TABS.map((x) => (
            <button
              key={x.key}
              className={tab === x.key ? 'sidenav__item sidenav__item--active' : 'sidenav__item'}
              onClick={() => setTab(x.key)}
            >
              <span className="sidenav__icon">{x.icon}</span>
              <span>{x.label}</span>
            </button>
          ))}
        </nav>

        <main className="shell-main">
          {tab === 'giris' && (
            <NewSale
              db={t.db}
              products={t.products}
              parties={t.parties}
              online={t.online}
              onCommitted={async () => { await t.refresh(); await t.sync(); }}
            />
          )}
          {tab === 'panel' && <Dashboard />}
          {tab === 'satis' && <SalesList sales={t.sales} />}
          {tab === 'cari' && <Cari />}
          {tab === 'stok' && <Stok />}
          {tab === 'ebelge' && <EBelge />}
          {tab === 'rapor' && <Raporlar />}
          {tab === 'ai' && <Ai />}
          {tab === 'cek' && <Cek />}
          {tab === 'kasa' && <Kasa />}
        </main>
      </div>
    </div>
  );
}

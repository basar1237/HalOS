import { useState } from 'react';
import { NewSale } from './NewSale';
import { SalesList } from './SalesList';
import { SyncBar } from './SyncBar';
import { useTerminal } from './useTerminal';
import { getTheme, setTheme, type Theme } from '../lib/theme';

interface Props {
  userName?: string;
  onLogout: () => void;
}

type View = 'sale' | 'sales';

export function Terminal({ userName, onLogout }: Props) {
  const t = useTerminal();
  const [theme, setThemeState] = useState<Theme>(getTheme());
  const [view, setView] = useState<View>('sale');

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
          <small>Hal Terminali · {userName ?? 'kullanıcı'}</small>
        </div>
        <div className="syncbar">
          {view === 'sale' ? (
            <button className="icon-btn" onClick={() => setView('sales')} title="Satış listesi">
              📋 Satışlar{t.sales.length ? ` (${t.sales.length})` : ''}
            </button>
          ) : (
            <button className="icon-btn" onClick={() => setView('sale')} title="Satış girişi">
              ← Satış Girişi
            </button>
          )}
          <SyncBar
            online={t.online}
            syncing={t.syncing}
            pending={t.pending}
            lastSync={t.lastSync}
            onSync={t.sync}
          />
          <button
            className="icon-btn"
            onClick={toggleTheme}
            title={theme === 'light' ? 'Koyu temaya geç' : 'Beyaz temaya geç'}
          >
            {theme === 'light' ? '🌙' : '☀️'}
          </button>
          <button className="ghost" onClick={onLogout}>
            Çıkış
          </button>
        </div>
      </header>

      <main className="content content--single">
        {view === 'sale' ? (
          <NewSale
            db={t.db}
            products={t.products}
            parties={t.parties}
            online={t.online}
            onCommitted={async () => {
              await t.refresh();
              await t.sync();
            }}
          />
        ) : (
          <SalesList sales={t.sales} />
        )}
      </main>
    </div>
  );
}

import { NewSale } from './NewSale';
import { SalesList } from './SalesList';
import { SyncBar } from './SyncBar';
import { useTerminal } from './useTerminal';

interface Props {
  userName?: string;
  onLogout: () => void;
}

export function Terminal({ userName, onLogout }: Props) {
  const t = useTerminal();

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          HalOS <small>Hal Terminali · {userName ?? 'kullanıcı'}</small>
        </div>
        <div className="syncbar">
          <SyncBar
            online={t.online}
            syncing={t.syncing}
            pending={t.pending}
            lastSync={t.lastSync}
            onSync={t.sync}
          />
          <button className="ghost" onClick={onLogout}>
            Çıkış
          </button>
        </div>
      </header>

      <main className="content">
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
        <SalesList sales={t.sales} />
      </main>
    </div>
  );
}

import type { SyncSummary } from '../lib/sync';

interface Props {
  online: boolean;
  syncing: boolean;
  pending: number;
  lastSync: SyncSummary | null;
  onSync: () => void;
}

export function SyncBar({ online, syncing, pending, lastSync, onSync }: Props) {
  return (
    <div className="syncbar">
      <span className="pill" title={online ? 'Bağlantı var' : 'Çevrimdışı — satışlar yerelde tutulur'}>
        <span className={`dot ${online ? 'online' : 'offline'}`} />
        {online ? 'Çevrimiçi' : 'Çevrimdışı'}
      </span>

      <span className="pill">
        Bekleyen
        {pending > 0 ? <span className="badge">{pending}</span> : <span>: 0</span>}
      </span>

      {lastSync && lastSync.errors.length > 0 && (
        <span className="pill" title={lastSync.errors.join('\n')} style={{ color: 'var(--danger)' }}>
          {lastSync.errors.length} hata
        </span>
      )}

      <button className="ghost" onClick={onSync} disabled={!online || syncing}>
        {syncing ? 'Senkron…' : 'Şimdi Senkronla'}
      </button>
    </div>
  );
}

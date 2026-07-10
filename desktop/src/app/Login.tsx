import { useState } from 'react';
import { gatewayApi, type ApiError } from '../lib/api';

interface Props {
  onLogin: (s: { token: string; userName?: string; tenantId?: string }) => void;
}

export function Login({ onLogin }: Props) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [tenantId, setTenantId] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const res = await gatewayApi.login(email, password, tenantId || undefined);
      if (!res.accessToken) throw { status: 500, message: 'Sunucu jeton döndürmedi' } as ApiError;
      onLogin({ token: res.accessToken, userName: res.userName ?? email, tenantId: res.tenantId ?? tenantId });
    } catch (err) {
      const msg = (err as ApiError)?.message ?? 'Giriş başarısız';
      setError(
        msg.includes('fetch') || msg.includes('Failed')
          ? 'Sunucuya ulaşılamadı — bağlantınızı kontrol edin.'
          : msg,
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="center">
      <form className="card" onSubmit={submit}>
        <h1>HalOS Hal Terminali</h1>
        <p className="sub">Offline-first hal satış terminali</p>
        <div style={{ marginBottom: 12 }}>
          <label>E-posta</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} autoFocus required />
        </div>
        <div style={{ marginBottom: 12 }}>
          <label>Parola</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>
        <div style={{ marginBottom: 16 }}>
          <label>Tenant (opsiyonel)</label>
          <input value={tenantId} onChange={(e) => setTenantId(e.target.value)} placeholder="işletme kimliği" />
        </div>
        <button type="submit" disabled={busy} style={{ width: '100%' }}>
          {busy ? 'Giriş yapılıyor…' : 'Giriş'}
        </button>
        {error && <div className="error">{error}</div>}
      </form>
    </div>
  );
}

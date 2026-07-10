import { useCallback, useEffect, useState } from 'react';
import { Login } from './app/Login';
import { Terminal } from './app/Terminal';
import { setAccessToken } from './lib/api';

interface Session {
  token: string;
  userName?: string;
  tenantId?: string;
}

const STORAGE_KEY = 'halos.terminal.session';

export function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [ready, setReady] = useState(false);

  // Oturum kurtarma: token yerelde saklanır (offline'da da terminal açılabilsin).
  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const s = JSON.parse(raw) as Session;
        setAccessToken(s.token);
        setSession(s);
      }
    } catch {
      /* yok say */
    }
    setReady(true);
  }, []);

  const handleLogin = useCallback((s: Session) => {
    setAccessToken(s.token);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(s));
    setSession(s);
  }, []);

  const handleLogout = useCallback(() => {
    setAccessToken(null);
    localStorage.removeItem(STORAGE_KEY);
    setSession(null);
  }, []);

  if (!ready) return null;

  return session ? (
    <Terminal userName={session.userName} onLogout={handleLogout} />
  ) : (
    <Login onLogin={handleLogin} />
  );
}

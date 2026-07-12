// Ortak veri-çekme hook'u — tab bileşenlerindeki tekrar eden useEffect+useState+catch kalıbını
// tek yerde toplar. reload ile yeniden çekilebilir; bileşen unmount olursa yarış (race) korunur.
import { useEffect, useState } from 'react';
import { apiGet } from './api';

export interface FetchState<T> {
  data: T | null;
  loading: boolean;
  error: string | null;
}

export function useFetch<T>(path: string): FetchState<T> {
  const [state, setState] = useState<FetchState<T>>({ data: null, loading: true, error: null });

  useEffect(() => {
    let cancelled = false;
    setState({ data: null, loading: true, error: null });
    apiGet<T>(path)
      .then((data) => { if (!cancelled) setState({ data, loading: false, error: null }); })
      .catch((e) => {
        if (!cancelled) setState({ data: null, loading: false, error: (e as { message?: string }).message ?? 'Veri alınamadı.' });
      });
    return () => { cancelled = true; };
  }, [path]);

  return state;
}

// Terminal teması — varsayılan BEYAZ (light). Kullanıcı seçimi yerelde saklanır.
const KEY = 'halos.terminal.theme';
export type Theme = 'light' | 'dark';

export function getTheme(): Theme {
  const v = (typeof localStorage !== 'undefined' && localStorage.getItem(KEY)) || 'light';
  return v === 'dark' ? 'dark' : 'light';
}

export function applyTheme(theme: Theme): void {
  if (typeof document !== 'undefined') {
    document.documentElement.dataset.theme = theme;
  }
}

export function setTheme(theme: Theme): void {
  try {
    localStorage.setItem(KEY, theme);
  } catch {
    /* yok say */
  }
  applyTheme(theme);
}

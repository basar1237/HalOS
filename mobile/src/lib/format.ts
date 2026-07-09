// Biçimlendirme yardımcıları — Hermes Intl'e bağımlı olmadan güvenilir TR gösterim.

/** Tutarı "1.234,56 ₺" biçiminde döndürür (nokta binlik, virgül ondalık). */
export function formatTRY(value: number): string {
  const fixed = Math.abs(value).toFixed(2);
  const [intPart, decPart] = fixed.split('.');
  const withThousands = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  const sign = value < 0 ? '-' : '';
  return `${sign}${withThousands},${decPart} ₺`;
}

/** ISO tarihi "GG.AA.YYYY" biçiminde döndürür. */
export function formatDate(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return '—';
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}

/** Bugünün tarihini yyyy-MM-dd döndürür (backend day param). */
export function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

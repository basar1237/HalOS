// TR biçimlendirme yardımcıları. Hermes/ICU'ya bağlı kalmadan güvenilir çalışması için
// para birimi elle biçimlenir (binlik nokta, ondalık virgül, ₺).

export function formatTRY(value: number): string {
  const rounded = Math.round(value * 100) / 100;
  const negative = rounded < 0;
  const abs = Math.abs(rounded);
  const [intPart, fracPart] = abs.toFixed(2).split('.');
  const withThousands = intPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  return `${negative ? '-' : ''}${withThousands},${fracPart} ₺`;
}

export function formatQuantity(value: number): string {
  // Gereksiz sıfırları at, ondalıkta virgül kullan.
  const s = Number.isInteger(value) ? String(value) : String(value).replace('.', ',');
  return s;
}

export function formatDateTimeTR(iso: string): string {
  // ISO → gün.ay.yıl saat:dakika (yerel değil, string tabanlı → deterministik).
  const m = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})/.exec(iso);
  if (!m) return iso;
  const [, y, mo, d, h, mi] = m;
  return `${d}.${mo}.${y} ${h}:${mi}`;
}

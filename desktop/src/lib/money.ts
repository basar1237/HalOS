// Para/miktar hesabı. Bu terminal offline'da yalnız TAHMİNİ tutar gösterir; yetkili hakediş
// hesabı sync sonrası backend SettlementCalculator'da yeniden yapılır (kaynak-doğruluk orada).
// Yine de gösterilen toplamlar backend ile tutarlı olsun diye banker's rounding (BK-2) kullanılır.

import type { SaleLineInput } from './types';

/**
 * Yarıyı-çifte yuvarlama (round-half-to-even) — backend BK-2 ile aynı davranış.
 * JS kayan nokta hatasına karşı küçük bir epsilon toleransı ile 0.5 sınırı yakalanır.
 */
export function bankersRound(value: number, decimals = 2): number {
  const factor = 10 ** decimals;
  const scaled = value * factor;
  const floor = Math.floor(scaled);
  const diff = scaled - floor;
  const EPS = 1e-9;
  let rounded: number;
  if (Math.abs(diff - 0.5) < EPS) {
    rounded = floor % 2 === 0 ? floor : floor + 1;
  } else {
    rounded = Math.round(scaled);
  }
  // -0 normalizasyonu
  return (rounded / factor) + 0;
}

/** Tek satır tutarı = miktar × birim fiyat (2 haneye yuvarlanmış). */
export function lineTotal(line: Pick<SaleLineInput, 'quantity' | 'unitPrice'>): number {
  return bankersRound(line.quantity * line.unitPrice);
}

/** Satışın brüt toplamı = satır tutarlarının toplamı. */
export function grossTotal(lines: ReadonlyArray<Pick<SaleLineInput, 'quantity' | 'unitPrice'>>): number {
  const sum = lines.reduce((acc, l) => acc + lineTotal(l), 0);
  return bankersRound(sum);
}

import { describe, expect, it } from 'vitest';
import { bankersRound, grossTotal, lineTotal } from './money';

describe('bankersRound (yarıyı-çifte, BK-2)', () => {
  it('0.5 sınırında en yakın çifte yuvarlar', () => {
    expect(bankersRound(2.5, 0)).toBe(2);
    expect(bankersRound(3.5, 0)).toBe(4);
    expect(bankersRound(0.125, 2)).toBe(0.12);
    expect(bankersRound(0.135, 2)).toBe(0.14);
  });

  it('0.5 dışında normal yuvarlar', () => {
    expect(bankersRound(2.4, 0)).toBe(2);
    expect(bankersRound(2.6, 0)).toBe(3);
    expect(bankersRound(1.005, 2)).toBe(1.0);
  });

  it('-0 yerine 0 döndürür', () => {
    expect(Object.is(bankersRound(-0.001, 2), 0)).toBe(true);
  });
});

describe('lineTotal / grossTotal', () => {
  it('satır tutarı miktar × fiyat', () => {
    expect(lineTotal({ quantity: 12.5, unitPrice: 8 })).toBe(100);
    expect(lineTotal({ quantity: 3, unitPrice: 4.5 })).toBe(13.5);
  });

  it('brüt toplam satırların toplamı', () => {
    expect(
      grossTotal([
        { quantity: 10, unitPrice: 5 },
        { quantity: 2, unitPrice: 3.25 },
      ]),
    ).toBe(56.5);
  });

  it('boş satırda 0', () => {
    expect(grossTotal([])).toBe(0);
  });
});

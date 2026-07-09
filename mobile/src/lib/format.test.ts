import { describe, expect, it } from 'vitest';

import { formatDate, formatTRY } from './format';

describe('formatTRY', () => {
  it('binlik nokta + ondalık virgül + ₺', () => {
    expect(formatTRY(1234.5)).toBe('1.234,50 ₺');
    expect(formatTRY(0)).toBe('0,00 ₺');
    expect(formatTRY(1000000)).toBe('1.000.000,00 ₺');
  });

  it('negatif bakiye işareti', () => {
    expect(formatTRY(-88)).toBe('-88,00 ₺');
  });
});

describe('formatDate', () => {
  it('ISO → GG.AA.YYYY', () => {
    expect(formatDate('2026-07-09T10:00:00Z')).toBe('09.07.2026');
  });

  it('geçersiz tarih → —', () => {
    expect(formatDate('geçersiz')).toBe('—');
  });
});

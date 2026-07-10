import { describe, expect, it } from 'vitest';
import { formatDateTimeTR, formatQuantity, formatTRY } from './format';

describe('formatTRY', () => {
  it('binlik nokta, ondalık virgül, ₺', () => {
    expect(formatTRY(1234567.5)).toBe('1.234.567,50 ₺');
    expect(formatTRY(0)).toBe('0,00 ₺');
    expect(formatTRY(1234.5)).toBe('1.234,50 ₺');
  });
  it('negatif', () => {
    expect(formatTRY(-1500)).toBe('-1.500,00 ₺');
  });
});

describe('formatQuantity', () => {
  it('tam sayı olduğu gibi', () => {
    expect(formatQuantity(12)).toBe('12');
  });
  it('ondalıkta virgül', () => {
    expect(formatQuantity(12.5)).toBe('12,5');
  });
});

describe('formatDateTimeTR', () => {
  it('ISO → gg.aa.yyyy ss:dd', () => {
    expect(formatDateTimeTR('2026-07-10T14:30:00.000Z')).toBe('10.07.2026 14:30');
  });
  it('geçersizse olduğu gibi', () => {
    expect(formatDateTimeTR('bozuk')).toBe('bozuk');
  });
});

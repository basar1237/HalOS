import { describe, expect, it } from 'vitest';

import {
  SALE_STATUS_LABEL,
  SALE_TERM_LABEL,
  SaleTerm,
  UNIT_LABEL,
  UnitOfMeasure,
} from './sales-api';

describe('sales enum sabitleri backend ile eşleşir', () => {
  it('SaleTerm Cash=1, Deferred=2', () => {
    expect(SaleTerm.Cash).toBe(1);
    expect(SaleTerm.Deferred).toBe(2);
  });

  it('UnitOfMeasure 1-5', () => {
    expect(UnitOfMeasure.Crate).toBe(1);
    expect(UnitOfMeasure.Kilogram).toBe(2);
    expect(UnitOfMeasure.Box).toBe(5);
  });
});

describe('sales etiket haritaları', () => {
  it('satış durumu Türkçe etiketleri', () => {
    expect(SALE_STATUS_LABEL[1]).toBe('Taslak');
    expect(SALE_STATUS_LABEL[2]).toBe('Tamamlandı');
    expect(SALE_STATUS_LABEL[3]).toBe('İptal');
  });

  it('vade ve birim etiketleri', () => {
    expect(SALE_TERM_LABEL[1]).toBe('Peşin');
    expect(UNIT_LABEL[1]).toBe('Kasa');
    expect(UNIT_LABEL[2]).toBe('Kg');
  });
});

import { describe, expect, it } from 'vitest';

import {
  INVOICE_STATUS_LABEL,
  label,
  PARTY_ROLE_LABEL,
  shortId,
} from '@/features/common/labels';

describe('label', () => {
  it('bilinen enum değerini haritadan çevirir', () => {
    expect(label(PARTY_ROLE_LABEL, 1)).toBe('Müstahsil');
    expect(label(PARTY_ROLE_LABEL, 2)).toBe('Alıcı');
    expect(label(INVOICE_STATUS_LABEL, 3)).toBe('Başarısız');
  });

  it('bilinmeyen değer için #N döner (çökmeden)', () => {
    expect(label(PARTY_ROLE_LABEL, 99)).toBe('#99');
  });
});

describe('shortId', () => {
  it('GUID ilk 8 hanesini döndürür', () => {
    expect(shortId('550e8400-e29b-41d4-a716-446655440000')).toBe('550e8400');
  });
});

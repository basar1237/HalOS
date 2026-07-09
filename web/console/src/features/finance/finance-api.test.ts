import { describe, expect, it } from 'vitest';

import { isMovementKind, MOVEMENT_TITLE, PaymentChannel } from './finance-api';

describe('isMovementKind', () => {
  it('geçerli tür kodlarını daraltır', () => {
    expect(isMovementKind('payment')).toBe(true);
    expect(isMovementKind('collection')).toBe(true);
    expect(isMovementKind('advance')).toBe(true);
  });

  it('geçersiz/eksik değeri reddeder', () => {
    expect(isMovementKind('foo')).toBe(false);
    expect(isMovementKind(null)).toBe(false);
  });
});

describe('finance sabitleri', () => {
  it('PaymentChannel backend ile eşleşir (Cash=1, Bank=2)', () => {
    expect(PaymentChannel.Cash).toBe(1);
    expect(PaymentChannel.Bank).toBe(2);
  });

  it('her hareket türünün Türkçe başlığı var', () => {
    expect(MOVEMENT_TITLE.payment).toBe('Müstahsile Ödeme');
    expect(MOVEMENT_TITLE.collection).toBe('Alıcıdan Tahsilat');
    expect(MOVEMENT_TITLE.advance).toBe('Avans');
  });
});

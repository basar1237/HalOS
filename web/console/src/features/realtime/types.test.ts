import { describe, expect, it } from 'vitest';

import { readNumber } from '@/features/realtime/types';

describe('readNumber', () => {
  it('geçerli sonlu sayıyı okur', () => {
    expect(readNumber({ netAmount: 1250.5 }, 'netAmount')).toBe(1250.5);
    expect(readNumber({ x: 0 }, 'x')).toBe(0);
  });

  it('eksik/sayı-olmayan/sonsuz değer için null döner', () => {
    expect(readNumber({ netAmount: 'x' }, 'netAmount')).toBeNull();
    expect(readNumber({}, 'netAmount')).toBeNull();
    expect(readNumber(null, 'netAmount')).toBeNull();
    expect(readNumber({ n: Infinity }, 'n')).toBeNull();
  });
});

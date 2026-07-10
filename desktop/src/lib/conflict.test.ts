import { describe, expect, it } from 'vitest';
import { compareVersion, mergeMasterList, resolveMaster } from './conflict';

const base = { id: '1', updatedAt: '2026-07-10T10:00:00.000Z' };

describe('compareVersion', () => {
  it('sayısal rowVersion karşılaştırır', () => {
    expect(compareVersion({ ...base, rowVersion: '5' }, { ...base, rowVersion: '3' })).toBe(1);
    expect(compareVersion({ ...base, rowVersion: '2' }, { ...base, rowVersion: '9' })).toBe(-1);
  });
  it('rowVersion yoksa updatedAt kullanır', () => {
    expect(
      compareVersion(
        { id: '1', updatedAt: '2026-07-10T12:00:00.000Z' },
        { id: '1', updatedAt: '2026-07-10T10:00:00.000Z' },
      ),
    ).toBe(1);
  });
});

describe('resolveMaster — son-yazan-kazanır', () => {
  it('yerel yoksa geleni alır', () => {
    const inc = { ...base, name: 'yeni' };
    expect(resolveMaster(undefined, inc)).toBe(inc);
  });
  it('gelen daha yeniyse geleni alır', () => {
    const local = { ...base, rowVersion: '1' };
    const inc = { ...base, rowVersion: '2' };
    expect(resolveMaster(local, inc)).toBe(inc);
  });
  it('eşitlikte bulut (gelen) kazanır', () => {
    const local = { ...base, rowVersion: '2' };
    const inc = { ...base, rowVersion: '2' };
    expect(resolveMaster(local, inc)).toBe(inc);
  });
  it('yerel daha yeniyse yereli korur', () => {
    const local = { ...base, rowVersion: '5' };
    const inc = { ...base, rowVersion: '2' };
    expect(resolveMaster(local, inc)).toBe(local);
  });
});

describe('mergeMasterList', () => {
  it('id bazında upsert + yerelde olup gelmeyeni korur', () => {
    const local = [
      { id: 'a', rowVersion: '1', updatedAt: '', name: 'A eski' },
      { id: 'b', rowVersion: '1', updatedAt: '', name: 'B yerel' },
    ];
    const incoming = [
      { id: 'a', rowVersion: '2', updatedAt: '', name: 'A yeni' },
      { id: 'c', rowVersion: '1', updatedAt: '', name: 'C yeni' },
    ];
    const merged = mergeMasterList(local, incoming);
    const byId = Object.fromEntries(merged.map((m) => [m.id, m.name]));
    expect(byId).toEqual({ a: 'A yeni', b: 'B yerel', c: 'C yeni' });
  });
});

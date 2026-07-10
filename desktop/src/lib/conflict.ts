// Master veri (ürün/oran/taraf) çakışma çözümü (docs/04 §5): son-yazan-kazanır + versiyon damgası.
// Mali kayıtlar append-only olduğundan burada yalnızca master veri ele alınır. Saf → test edilir.

export interface Versioned {
  id: string;
  rowVersion?: string | null;
  updatedAt: string; // ISO 8601
}

/**
 * İki sürümü karşılaştırır. Pozitif → a daha yeni; negatif → b daha yeni; 0 → eşit.
 * Öncelik rowVersion (sayısal karşılaştırılabilirse sayısal, değilse sözlüksel);
 * rowVersion yoksa updatedAt (ISO string sözlüksel = kronolojik).
 */
export function compareVersion(a: Versioned, b: Versioned): number {
  if (a.rowVersion != null && b.rowVersion != null) {
    const na = Number(a.rowVersion);
    const nb = Number(b.rowVersion);
    if (!Number.isNaN(na) && !Number.isNaN(nb)) return Math.sign(na - nb);
    return a.rowVersion < b.rowVersion ? -1 : a.rowVersion > b.rowVersion ? 1 : 0;
  }
  return a.updatedAt < b.updatedAt ? -1 : a.updatedAt > b.updatedAt ? 1 : 0;
}

/**
 * Buluttan gelen bir kaydı yerel ile birleştirir (son-yazan-kazanır).
 * Yerel yoksa veya gelen daha yeni/eşitse gelen kazanır (sunucu otoriter, eşitlikte de bulut).
 */
export function resolveMaster<T extends Versioned>(local: T | undefined, incoming: T): T {
  if (!local) return incoming;
  return compareVersion(incoming, local) >= 0 ? incoming : local;
}

/**
 * Buluttan çekilen bir listeyi yerel önbellekle birleştirir.
 * id bazında upsert; her id için son-yazan-kazanır. Yerelde olup gelmeyen kayıtlar korunur.
 */
export function mergeMasterList<T extends Versioned>(
  localList: ReadonlyArray<T>,
  incomingList: ReadonlyArray<T>,
): T[] {
  const byId = new Map<string, T>();
  for (const l of localList) byId.set(l.id, l);
  for (const inc of incomingList) {
    byId.set(inc.id, resolveMaster(byId.get(inc.id), inc));
  }
  return [...byId.values()];
}

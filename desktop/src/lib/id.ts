// İstemci üretimli operationId (idempotency anahtarı, docs/04 §5).
// crypto.randomUUID modern WebView'larda (Tauri WKWebView/WebView2) mevcuttur;
// yoksa RFC-4122 v4 üreten güvenli bir yedeğe düşer.

export function newOperationId(): string {
  const c = globalThis.crypto;
  if (c && typeof c.randomUUID === 'function') {
    return c.randomUUID();
  }
  // Yedek: crypto.getRandomValues ile v4
  const bytes = new Uint8Array(16);
  if (c && typeof c.getRandomValues === 'function') {
    c.getRandomValues(bytes);
  } else {
    throw new Error('Güvenli rastgelelik kaynağı yok — operationId üretilemez.');
  }
  bytes[6] = (bytes[6] & 0x0f) | 0x40; // sürüm 4
  bytes[8] = (bytes[8] & 0x3f) | 0x80; // varyant
  const hex = [...bytes].map((b) => b.toString(16).padStart(2, '0'));
  return `${hex.slice(0, 4).join('')}-${hex.slice(4, 6).join('')}-${hex
    .slice(6, 8)
    .join('')}-${hex.slice(8, 10).join('')}-${hex.slice(10, 16).join('')}`;
}

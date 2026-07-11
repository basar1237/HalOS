// Soğuk zincir seed — odalar + sensör okumaları (biri eşik aşımı/alarm). node seed-coldchain.mjs
const BASE = process.env.HALOS_API ?? 'http://localhost:5000';
let TOKEN = '';
async function api(path, method = 'GET', body) {
  const res = await fetch(`${BASE}${path}`, { method, headers: { 'Content-Type': 'application/json', ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}) }, body: body ? JSON.stringify(body) : undefined });
  const t = await res.text(); let d; try { d = t ? JSON.parse(t) : null; } catch { d = t; }
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${t.slice(0, 160)}`);
  return d;
}
const rndf = (a, b) => Math.round((Math.random() * (b - a) + a) * 10) / 10;

const UNITS = [
  { name: 'Soğuk Oda 1 (Sebze)', minTempC: 2, maxTempC: 8, base: [3, 7], breach: false },
  { name: 'Soğuk Oda 2 (Meyve)', minTempC: 0, maxTempC: 6, base: [1, 5], breach: false },
  { name: 'Muz Olgunlaştırma', minTempC: 13, maxTempC: 18, base: [14, 17], breach: false },
  { name: 'Soğuk Oda 3 (Yedek)', minTempC: 2, maxTempC: 8, base: [3, 7], breach: true }, // arıza: son okumalar yüksek
];

async function main() {
  TOKEN = (await api('/api/identity/auth/login', 'POST', { email: 'patron@demo.com', password: 'Patron1234!' })).accessToken;
  console.log('✓ Giriş.');
  for (const u of UNITS) {
    try {
      const { id } = await api('/api/coldchain/cold-storage-units', 'POST', { name: u.name, minTempC: u.minTempC, maxTempC: u.maxTempC });
      // Son 24 saat, saat başı okuma
      let ok = 0;
      for (let h = 24; h >= 0; h--) {
        const occurredAt = new Date(Date.now() - h * 3600000).toISOString();
        // Arızalı oda: son 3 saatte eşik aşımı (yüksek sıcaklık → alarm)
        const temp = (u.breach && h <= 3) ? rndf(u.maxTempC + 2, u.maxTempC + 5) : rndf(u.base[0], u.base[1]);
        await api(`/api/coldchain/cold-storage-units/${id}/readings`, 'POST', {
          readingId: crypto.randomUUID(), temperatureC: temp, humidityPercent: rndf(80, 92), occurredAt,
        });
        ok++;
      }
      console.log(`✓ ${u.name}: ${ok} okuma${u.breach ? ' (ALARM: eşik aşımı)' : ''}`);
    } catch (e) { console.log(`! ${u.name}: ${e.message}`); }
  }
  console.log('🎉 Soğuk zincir seed bitti.');
}
main().catch((e) => { console.error('HATA:', e.message); process.exit(1); });

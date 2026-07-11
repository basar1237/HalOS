// Mevcut ürün/taraflara karşı SATIŞ basar (temel veriyi çoğaltmaz). node seed-sales.mjs
const BASE = process.env.HALOS_API ?? 'http://localhost:5000';
let TOKEN = '';

async function api(path, method = 'GET', body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: { 'Content-Type': 'application/json', ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}) },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data; try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${text.slice(0, 160)}`);
  return data;
}
const rnd = (a, b) => Math.floor(Math.random() * (b - a + 1)) + a;
const pick = (a) => a[rnd(0, a.length - 1)];
const money = (a, b) => Math.round((Math.random() * (b - a) + a) * 4) / 4;
const PRICE = { Domates: [28, 42], Salatalık: [18, 30], 'Sivri Biber': [35, 55], Patlıcan: [22, 38], Kabak: [16, 28], Patates: [11, 18], 'Soğan Kuru': [9, 15], 'Elma Starking': [17, 26], Portakal: [14, 22], Muz: [38, 52], Limon: [25, 40], 'Üzüm Sofralık': [30, 48] };

async function main() {
  TOKEN = (await api('/api/identity/auth/login', 'POST', { email: 'patron@demo.com', password: 'Patron1234!' })).accessToken;
  console.log('✓ Giriş.');
  const prods = (await api('/api/inventory/products?page=1&pageSize=200')).items;
  const parties = (await api('/api/party/parties?page=1&pageSize=200')).items;
  const producers = parties.filter((p) => p.roles?.includes(1)).map((p) => p.id);
  const buyers = parties.filter((p) => p.roles?.includes(2)).map((p) => p.id);
  console.log(`✓ ${prods.length} ürün, ${producers.length} müstahsil, ${buyers.length} alıcı.`);
  if (!prods.length || !producers.length || !buyers.length) throw new Error('Temel veri eksik.');

  const N = 45; let ok = 0;
  for (let i = 0; i < N; i++) {
    try {
      const soldAt = new Date(Date.now() - rnd(0, 20) * 86400000);
      soldAt.setHours(rnd(6, 16), rnd(0, 59), 0, 0);
      const sale = await api('/api/sales/sales', 'POST', {
        buyerPartyId: pick(buyers), producerPartyId: pick(producers), consignmentId: null,
        soldAt: soldAt.toISOString(), isWithinMarket: Math.random() > 0.25,
        operationId: crypto.randomUUID(), term: Math.random() > 0.5 ? 1 : 2,
      });
      for (let l = 0, n = rnd(1, 3); l < n; l++) {
        const p = pick(prods);
        const range = PRICE[p.name] ?? [15, 40];
        await api(`/api/sales/sales/${sale.id}/lines`, 'POST', {
          productId: p.id, quantity: rnd(5, 80), unit: p.defaultUnit, unitPrice: money(range[0], range[1]),
        });
      }
      await api(`/api/sales/sales/${sale.id}/complete`, 'POST');
      ok++; process.stdout.write('.');
    } catch (e) { console.log(`\n ! ${i + 1}: ${e.message}`); }
  }
  console.log(`\n✓ ${ok}/${N} satış tamamlandı. Konsolu yenile (http://localhost:3001).`);
}
main().catch((e) => { console.error('HATA:', e.message); process.exit(1); });

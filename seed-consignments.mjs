// Mal geliş (konsinye) basar → stok girişi + künye/pasaport. node seed-consignments.mjs
const BASE = process.env.HALOS_API ?? 'http://localhost:5000';
let TOKEN = '';
async function api(path, method = 'GET', body) {
  const res = await fetch(`${BASE}${path}`, { method, headers: { 'Content-Type': 'application/json', ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}) }, body: body ? JSON.stringify(body) : undefined });
  const t = await res.text(); let d; try { d = t ? JSON.parse(t) : null; } catch { d = t; }
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${t.slice(0, 160)}`);
  return d;
}
const rnd = (a, b) => Math.floor(Math.random() * (b - a + 1)) + a;
const pick = (a) => a[rnd(0, a.length - 1)];
async function main() {
  TOKEN = (await api('/api/identity/auth/login', 'POST', { email: 'patron@demo.com', password: 'Patron1234!' })).accessToken;
  const prods = (await api('/api/inventory/products?page=1&pageSize=200')).items;
  const producers = (await api('/api/party/parties?page=1&pageSize=200')).items.filter((p) => p.roles?.includes(1)).map((p) => p.id);
  console.log(`✓ ${prods.length} ürün, ${producers.length} müstahsil.`);
  const N = 20; let ok = 0;
  for (let i = 0; i < N; i++) {
    try {
      const r = new Date(Date.now() - rnd(0, 20) * 86400000);
      const items = Array.from({ length: rnd(1, 3) }, () => { const p = pick(prods); return { productId: p.id, quantity: rnd(20, 200), unit: p.defaultUnit }; });
      await api('/api/sales/consignments', 'POST', { producerPartyId: pick(producers), receivedAt: r.toISOString(), dispatchNoteRef: `İRS-${rnd(1000, 9999)}`, items });
      ok++; process.stdout.write('.');
    } catch (e) { console.log(`\n ! ${i + 1}: ${e.message}`); }
  }
  console.log(`\n✓ ${ok}/${N} mal geliş.`);
}
main().catch((e) => { console.error('HATA:', e.message); process.exit(1); });

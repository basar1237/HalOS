// Çek/Senet + Kasa demo verisi basar. node seed-finance.mjs
const BASE = process.env.HALOS_API ?? 'http://localhost:5000';
let TOKEN = '';
async function api(path, method = 'GET', body) {
  const res = await fetch(`${BASE}${path}`, { method, headers: { 'Content-Type': 'application/json', ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}) }, body: body ? JSON.stringify(body) : undefined });
  const t = await res.text(); let d; try { d = t ? JSON.parse(t) : null; } catch { d = t; }
  if (!res.ok) throw new Error(`${method} ${path} → ${res.status}: ${t.slice(0, 140)}`);
  return d;
}
const rnd = (a, b) => Math.floor(Math.random() * (b - a + 1)) + a;
const pick = (a) => a[rnd(0, a.length - 1)];
const daysFromNow = (n) => { const d = new Date(); d.setDate(d.getDate() + n); return d.toISOString(); };

const BANKS = ['Ziraat Bankasi', 'Is Bankasi', 'Garanti', 'Akbank', 'Yapi Kredi', 'Halkbank'];

async function main() {
  TOKEN = (await api('/api/identity/auth/login', 'POST', { email: 'patron@demo.com', password: 'Patron1234!' })).accessToken;
  console.log('✓ Giriş.');

  // --- Kasalar ---
  const existing = await api('/api/finance/cash-registers');
  if (existing.length < 2) {
    for (const [name, kind] of [['Merkez Kasa', 1], ['Rehin Kasa', 2], ['Banka Kasa', 1]]) {
      try { await api('/api/finance/cash-registers', 'POST', { name, kind }); } catch (e) { /* zaten var olabilir */ }
    }
  }
  const regs = await api('/api/finance/cash-registers');
  // Her kasaya birkaç hareket
  let mv = 0;
  for (const r of regs) {
    for (let i = 0; i < rnd(2, 5); i++) {
      const dir = Math.random() > 0.35 ? 1 : 2;
      try { await api(`/api/finance/cash-registers/${r.id}/movements`, 'POST', { direction: dir, amount: rnd(500, 12000), description: dir === 1 ? 'Tahsilat' : 'Odeme', occurredAt: daysFromNow(-rnd(0, 20)) }); mv++; } catch (e) {}
    }
  }
  console.log(`✓ ${regs.length} kasa, ${mv} hareket.`);

  // --- Çek / Senet ---
  const STATUSES = [1, 1, 1, 2, 3, 4]; // çoğu portföyde, bazıları tahsile/tahsil/karşılıksız
  let ok = 0;
  for (let i = 0; i < 14; i++) {
    try {
      const kind = Math.random() > 0.35 ? 1 : 2; // çoğu çek
      const direction = Math.random() > 0.3 ? 1 : 2; // çoğu alınan
      const due = daysFromNow(rnd(-10, 45)); // bazıları vadesi geçmiş → ajanda
      const reg = await api('/api/finance/cheques', 'POST', {
        kind, direction, partyId: null, bankName: pick(BANKS),
        serialNo: String(rnd(100000, 999999)), amount: rnd(3000, 50000),
        issueDate: daysFromNow(-rnd(5, 30)), dueDate: due, note: null,
      });
      // Bazılarının durumunu ilerlet
      const target = pick(STATUSES);
      if (target !== 1) {
        try { await api(`/api/finance/cheques/${reg}/status`, 'POST', { newStatus: target }); } catch (e) {}
      }
      ok++;
    } catch (e) { console.log(' ! ' + e.message); }
  }
  console.log(`✓ ${ok} çek/senet.`);
  console.log('🎉 Finans demo verisi bitti. 1420 → Çek/Kasa/Ajanda sekmelerini yenile.');
}
main().catch((e) => { console.error('HATA:', e.message); process.exit(1); });

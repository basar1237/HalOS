// HalOS demo veri tohumlayıcı — gerçekçi Türkçe hal verisi basar (ürün, müstahsil,
// alıcı, satış). Satış tamamlama komisyon/stopaj/hakediş + künye + cari'yi otomatik
// oluşturur. Çalıştır: node seed-demo.mjs
// Not: dev tool; birden çok çalıştırılırsa veri çoğaltır (demo için sorun değil).

const BASE = process.env.HALOS_API ?? 'http://localhost:5000';
const EMAIL = 'patron@demo.com';
const PASSWORD = 'Patron1234!';

let TOKEN = '';

async function api(path, method = 'POST', body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data;
  try { data = text ? JSON.parse(text) : null; } catch { data = text; }
  if (!res.ok) {
    throw new Error(`${method} ${path} → ${res.status}: ${text.slice(0, 200)}`);
  }
  return data;
}

const rnd = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min;
const pick = (arr) => arr[rnd(0, arr.length - 1)];
const money = (min, max) => Math.round((Math.random() * (max - min) + min) * 4) / 4; // .25 adım

// --- Katalog ---
const PRODUCTS = [
  { name: 'Domates', category: 'Sebze', defaultUnit: 1, price: [28, 42] },
  { name: 'Salatalık', category: 'Sebze', defaultUnit: 1, price: [18, 30] },
  { name: 'Sivri Biber', category: 'Sebze', defaultUnit: 1, price: [35, 55] },
  { name: 'Patlıcan', category: 'Sebze', defaultUnit: 1, price: [22, 38] },
  { name: 'Kabak', category: 'Sebze', defaultUnit: 1, price: [16, 28] },
  { name: 'Patates', category: 'Sebze', defaultUnit: 3, price: [11, 18] },
  { name: 'Soğan Kuru', category: 'Sebze', defaultUnit: 3, price: [9, 15] },
  { name: 'Elma Starking', category: 'Meyve', defaultUnit: 1, price: [17, 26] },
  { name: 'Portakal', category: 'Meyve', defaultUnit: 1, price: [14, 22] },
  { name: 'Muz', category: 'Meyve', defaultUnit: 2, price: [38, 52] },
  { name: 'Limon', category: 'Meyve', defaultUnit: 1, price: [25, 40] },
  { name: 'Üzüm Sofralık', category: 'Meyve', defaultUnit: 1, price: [30, 48] },
];

const PRODUCERS = [
  'Mehmet Çiftçi', 'Ali Bahçıvan', 'Hüseyin Üretici', 'Ramazan Toprak',
  'İbrahim Yeşil', 'Osman Ekinci', 'Mustafa Bağcı', 'Yusuf Tarla',
];
const BUYERS = [
  'Ahmet Manav', 'Ayşe Toptancı Gıda', 'Hasan Market', 'Fatma Sebze Ltd.',
  'Kemal Bakkaliye', 'Zeynep Pazarcı', 'Murat Restoran', 'Selin Market Zinciri',
  'Cem Manav', 'Derya Gıda A.Ş.',
];

function fakeTckn() { return String(rnd(10, 99)) + String(rnd(100000000, 999999999)); }
function fakeVkn() { return String(rnd(1000000000, 9999999999)); }

async function main() {
  console.log(`→ Giriş yapılıyor (${EMAIL})…`);
  const login = await api('/api/identity/auth/login', 'POST', { email: EMAIL, password: PASSWORD });
  TOKEN = login.accessToken;
  console.log('✓ Token alındı.\n');

  // Ürünler
  console.log('→ Ürünler oluşturuluyor…');
  const productIds = [];
  for (const p of PRODUCTS) {
    try {
      const r = await api('/api/inventory/products', 'POST', {
        name: p.name, category: p.category, defaultUnit: p.defaultUnit,
      });
      productIds.push({ id: r.id, ...p });
      process.stdout.write('.');
    } catch (e) { console.log(`\n  ! ${p.name}: ${e.message}`); }
  }
  console.log(`\n✓ ${productIds.length} ürün.\n`);

  // Müstahsiller (Producer=1, stopaj profili zorunlu)
  console.log('→ Müstahsiller oluşturuluyor…');
  const producerIds = [];
  for (const name of PRODUCERS) {
    try {
      const r = await api('/api/party/parties', 'POST', {
        displayName: name, tckn: fakeTckn(), vkn: null, taxOffice: null,
        phone: `05${rnd(30, 55)}${rnd(1000000, 9999999)}`, address: null,
        keepsRecords: false,
        withholdingProfile: { agriWithholdingRate: 0.02, farmerSskRate: 0.01 },
        roles: [1],
      });
      producerIds.push(r.id);
      process.stdout.write('.');
    } catch (e) { console.log(`\n  ! ${name}: ${e.message}`); }
  }
  console.log(`\n✓ ${producerIds.length} müstahsil.\n`);

  // Alıcılar (Buyer=2)
  console.log('→ Alıcılar oluşturuluyor…');
  const buyerIds = [];
  for (const name of BUYERS) {
    try {
      const r = await api('/api/party/parties', 'POST', {
        displayName: name, tckn: null, vkn: fakeVkn(), taxOffice: 'Merkez',
        phone: `05${rnd(30, 55)}${rnd(1000000, 9999999)}`, address: null,
        keepsRecords: true, withholdingProfile: null, roles: [2],
      });
      buyerIds.push(r.id);
      process.stdout.write('.');
    } catch (e) { console.log(`\n  ! ${name}: ${e.message}`); }
  }
  console.log(`\n✓ ${buyerIds.length} alıcı.\n`);

  if (!productIds.length || !producerIds.length || !buyerIds.length) {
    console.log('! Yeterli temel veri yok, satış atlanıyor.');
    return;
  }

  // Satışlar — son 20 güne yayılmış
  console.log('→ Satışlar oluşturuluyor (oluştur → satır → tamamla)…');
  const SALE_COUNT = 45;
  let ok = 0;
  for (let i = 0; i < SALE_COUNT; i++) {
    try {
      const daysAgo = rnd(0, 20);
      const soldAt = new Date(Date.now() - daysAgo * 86400000);
      soldAt.setHours(rnd(6, 16), rnd(0, 59), 0, 0);
      const header = {
        buyerPartyId: pick(buyerIds),
        producerPartyId: pick(producerIds),
        consignmentId: null,
        soldAt: soldAt.toISOString(),
        isWithinMarket: Math.random() > 0.25,
        operationId: crypto.randomUUID(),
        term: Math.random() > 0.5 ? 1 : 2,
      };
      const sale = await api('/api/sales/sales', 'POST', header);
      const lineCount = rnd(1, 3);
      for (let l = 0; l < lineCount; l++) {
        const prod = pick(productIds);
        await api(`/api/sales/sales/${sale.id}/lines`, 'POST', {
          productId: prod.id,
          quantity: rnd(5, 80),
          unit: prod.defaultUnit,
          unitPrice: money(prod.price[0], prod.price[1]),
        });
      }
      await api(`/api/sales/sales/${sale.id}/complete`, 'POST');
      ok++;
      process.stdout.write('.');
    } catch (e) { console.log(`\n  ! satış ${i + 1}: ${e.message}`); }
  }
  console.log(`\n✓ ${ok}/${SALE_COUNT} satış tamamlandı.\n`);
  console.log('🎉 Seed bitti. Konsolu (http://localhost:3001) yenile.');
}

main().catch((e) => { console.error('HATA:', e.message); process.exit(1); });

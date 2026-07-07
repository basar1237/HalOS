// Kontrol paneli — boş kartlar (iskelet). Kartlar ileriki fazda gerçek verilerle dolar.

interface DashboardCard {
  title: string;
}

// Kartlar docs/02 bağlamlarına ve günlük akışa (docs/02 §5) karşılık gelen özet alanlar.
const CARDS: DashboardCard[] = [
  { title: 'Günlük Satış' },
  { title: 'Bekleyen Hakediş' },
  { title: 'Açık Cari Bakiye' },
  { title: 'Bugünkü Mal Geliş' },
  { title: 'Bekleyen e-Belge' },
  { title: 'Soğuk Zincir Uyarıları' },
];

export default function DashboardPage() {
  return (
    <div>
      <h1 className="page-title">Kontrol Paneli</h1>
      <div className="card-grid">
        {CARDS.map((card) => (
          <section key={card.title} className="card">
            <p className="card__title">{card.title}</p>
            <p className="card__placeholder">Veri yok</p>
          </section>
        ))}
      </div>
    </div>
  );
}

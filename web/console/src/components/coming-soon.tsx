// Henüz bağlanmamış modüller için dürüst yer tutucu. Sidebar navigasyonu eksiksiz olsun diye
// her modülün bir sayfası vardır; veri/servis hazır olunca bu bileşen gerçek içerikle değişir.

export function ComingSoon({ title, note }: { title: string; note: string }) {
  return (
    <div>
      <h1 className="page-title">{title}</h1>
      <div className="coming-soon">
        <p className="coming-soon__badge">Yakında</p>
        <p className="coming-soon__note">{note}</p>
      </div>
    </div>
  );
}

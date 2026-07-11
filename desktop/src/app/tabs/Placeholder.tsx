export function Placeholder({ title, faz, features }: { title: string; faz: string; features: string[] }) {
  return (
    <section className="panel">
      <h2>{title}</h2>
      <div style={{ border: '1px dashed var(--line)', borderRadius: 10, padding: 32, textAlign: 'center', marginTop: 8 }}>
        <div style={{ fontSize: 13, color: 'var(--muted)', fontWeight: 600, marginBottom: 10 }}>{faz} — yapım aşamasında</div>
        <p className="muted" style={{ maxWidth: 480, margin: '0 auto 16px' }}>
          Bu modül master planda (docs/11) tanımlı ve sırada. Planlanan özellikler:
        </p>
        <ul style={{ textAlign: 'left', maxWidth: 420, margin: '0 auto', color: 'var(--muted)', fontSize: 14, lineHeight: 1.8 }}>
          {features.map((f) => <li key={f}>{f}</li>)}
        </ul>
      </div>
    </section>
  );
}

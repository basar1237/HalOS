'use client';

// AI Önerileri — proaktif AI ajanı (docs/06 S3.2). "Öneri Al" ile ai-gateway /ai/insights
// çağrılır; ERP verisinden öncelikli uyarı/aksiyon özeti gösterilir. API anahtarı yoksa
// ai-gateway stub yanıt döner (gerçek Claude çağrısı yapılmadan servis çalışır).

import { useState } from 'react';

import { getInsights, type AiInsights } from '@/features/ai/ai-api';
import { isApiError } from '@/lib/api-client';

export default function AiInsightsPage() {
  const [insights, setInsights] = useState<AiInsights | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleFetch() {
    setLoading(true);
    setError(null);
    try {
      setInsights(await getInsights());
    } catch (err) {
      setError(isApiError(err) ? err.message : 'Öneriler alınamadı.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div>
      <div className="page-head">
        <h1 className="page-title">AI Önerileri</h1>
        <div className="btn-group">
          <button className="btn-primary btn-sm" onClick={handleFetch} disabled={loading}>
            {loading ? 'Analiz ediliyor…' : 'Öneri Al'}
          </button>
        </div>
      </div>

      <p className="muted">
        Proaktif AI ajanı, güncel satış ve cari verilerinizi inceleyip dikkat etmeniz gereken
        durumları ve önerilen aksiyonları öncelik sırasına göre özetler (docs/06 S3.2).
      </p>

      {error ? <div className="form-error">{error}</div> : null}

      {insights ? (
        <div className="form-card" style={{ marginTop: 16 }}>
          <pre
            style={{
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              fontFamily: 'inherit',
              margin: 0,
            }}
          >
            {insights.summary}
          </pre>
          <p className="muted" style={{ marginTop: 12 }}>
            Model: {insights.model} · Kaynaklar: {insights.usedSources.join(', ') || '—'}
          </p>
        </div>
      ) : (
        !loading && (
          <p className="muted" style={{ marginTop: 16 }}>
            Henüz öneri alınmadı. “Öneri Al” ile başlayın.
          </p>
        )
      )}
    </div>
  );
}

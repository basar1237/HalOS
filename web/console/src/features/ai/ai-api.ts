// AI Gateway çağrıları — Gateway /ai/* → ai-gateway servisi (ADR-002, SALT-OKUMA).
// Proaktif öneriler (S3.2): ERP verisinden öncelikli uyarı/aksiyon özeti.

import { apiClient } from '@/lib/api-client';

export interface AiInsights {
  summary: string;
  usedSources: string[];
  model: string;
}

/** Backend yanıtı snake_case (used_sources) → camelCase'e çevrilir. */
interface RawInsights {
  summary: string;
  used_sources: string[];
  model: string;
}

export async function getInsights(): Promise<AiInsights> {
  const raw = await apiClient.post<RawInsights>('/ai/insights', {});
  return {
    summary: raw.summary,
    usedSources: raw.used_sources ?? [],
    model: raw.model,
  };
}

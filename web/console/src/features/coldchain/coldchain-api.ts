// ColdChain (Soğuk Zincir) çağrıları — Gateway üzerinden /api/coldchain (docs/04 §6, S3.1).
// Okuma: soğuk oda listesi (son sıcaklık + eşikler). Yazma: yeni oda tanımla, eşik güncelle, okuma gönder.

import { apiClient } from '@/lib/api-client';

/** Soğuk hava deposu okuma modeli (backend ColdStorageUnitDto). */
export interface ColdStorageUnit {
  id: string;
  name: string;
  minTempC: number;
  maxTempC: number;
  isActive: boolean;
  latestTemperatureC: number | null;
  latestReadingAt: string | null;
  readingCount: number;
}

export interface RegisterColdStorageUnitRequest {
  name: string;
  minTempC: number;
  maxTempC: number;
}

export function registerColdStorageUnit(
  request: RegisterColdStorageUnitRequest,
): Promise<{ id: string }> {
  return apiClient.post('/api/coldchain/cold-storage-units', request);
}

export interface RecordReadingRequest {
  readingId: string;
  temperatureC: number;
  humidityPercent?: number | null;
  occurredAt?: string;
}

export function recordReading(unitId: string, request: RecordReadingRequest): Promise<unknown> {
  return apiClient.post(`/api/coldchain/cold-storage-units/${unitId}/readings`, request);
}

/**
 * Son okuma izin verilen aralığın dışında mı (eşik ihlali). Okuma yoksa false (bilinmiyor).
 * Backend ile aynı kural: temp > max ya da temp < min.
 */
export function isBreaching(unit: ColdStorageUnit): boolean {
  if (unit.latestTemperatureC == null) return false;
  return unit.latestTemperatureC > unit.maxTempC || unit.latestTemperatureC < unit.minTempC;
}

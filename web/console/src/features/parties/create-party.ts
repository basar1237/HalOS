// Taraf oluşturma isteği + API çağrısı — backend CreatePartyRequest ile birebir.
// POST /api/party/parties (yazma yetkisi: Patron/Yönetici). Domain kuralı: Producer rolü
// stopaj profili gerektirir (backend zorlar; eksikse hata mesajı gösterilir).

import { apiClient } from '@/lib/api-client';

export interface WithholdingProfileInput {
  agriWithholdingRate: number;
  farmerSskRate: number;
}

export interface CreatePartyRequest {
  displayName: string;
  tckn: string | null;
  vkn: string | null;
  taxOffice: string | null;
  phone: string | null;
  address: string | null;
  keepsRecords: boolean;
  withholdingProfile: WithholdingProfileInput | null;
  roles: number[];
}

/** Oluşturulan tarafın kimliğini döndürür (backend { id }). */
export function createParty(request: CreatePartyRequest): Promise<{ id: string }> {
  return apiClient.post<{ id: string }>('/api/party/parties', request);
}

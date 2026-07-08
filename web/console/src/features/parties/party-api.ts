// Taraf okuma/güncelleme/pasifleştirme çağrıları (Gateway üzerinden).
//   GET    /api/party/parties/{id}   → Party (detay)
//   PUT    /api/party/parties/{id}   → 204 (ad/vergi dairesi/telefon/adres/defter/stopaj)
//   DELETE /api/party/parties/{id}   → 204 (soft-delete: pasifleştir)
// NOT: Güncelleme rolleri ve TCKN/VKN'yi DEĞİŞTİRMEZ (oluşturmada belirlenir; rol için ayrı uç).

import { apiClient } from '@/lib/api-client';
import type { Party } from '@/shared/entities';
import type { WithholdingProfileInput } from './create-party';

export interface UpdatePartyRequest {
  displayName: string;
  taxOffice: string | null;
  phone: string | null;
  address: string | null;
  keepsRecords: boolean;
  withholdingProfile: WithholdingProfileInput | null;
}

export function getParty(id: string): Promise<Party> {
  return apiClient.get<Party>(`/api/party/parties/${id}`);
}

export function updateParty(id: string, request: UpdatePartyRequest): Promise<void> {
  return apiClient.put<void>(`/api/party/parties/${id}`, request);
}

export function deactivateParty(id: string): Promise<void> {
  return apiClient.delete<void>(`/api/party/parties/${id}`);
}

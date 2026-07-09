// Backend enum tamsayı değerlerinin Türkçe etiketleri. Enum'lar JSON'da INT olarak gelir
// (System.Text.Json varsayılanı) — değerler backend Domain/Enums tanımlarıyla birebir.

/** Party PartyRoleType (1-tabanlı). */
export const PARTY_ROLE_LABEL: Record<number, string> = {
  1: 'Müstahsil',
  2: 'Alıcı',
  3: 'Tüccar',
  4: 'Konsinyeci',
};

/** Integration InvoiceStatus. */
export const INVOICE_STATUS_LABEL: Record<number, string> = {
  1: 'Taslak',
  2: 'Düzenlendi',
  3: 'Başarısız',
  4: 'İptal',
};

/** Integration InvoiceScenario. */
export const INVOICE_SCENARIO_LABEL: Record<number, string> = {
  1: 'HAL',
};

/** Integration InvoiceType. */
export const INVOICE_TYPE_LABEL: Record<number, string> = {
  1: 'Komisyon',
  2: 'Satış',
};

/** Integration ProducerReceiptStatus (e-MM). */
export const RECEIPT_STATUS_LABEL: Record<number, string> = {
  1: 'Taslak',
  2: 'Kesildi',
  3: 'Başarısız',
  4: 'İptal',
};

/** Integration HksNotificationStatus. */
export const HKS_STATUS_LABEL: Record<number, string> = {
  1: 'Taslak',
  2: 'Bildirildi',
  3: 'Başarısız',
  4: 'İptal',
};

/** Bilinmeyen enum değeri için güvenli etiket. */
export function label(map: Record<number, string>, value: number): string {
  return map[value] ?? `#${value}`;
}

/** GUID'i kısa gösterim (ilk 8 hane) — okuma modeli isim çözümü gelene kadar. */
export function shortId(id: string): string {
  return id.slice(0, 8);
}

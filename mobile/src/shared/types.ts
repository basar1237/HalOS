// Paylaşılan tipler — web konsoluyla aynı backend sözleşmeleri (camelCase JSON, Gateway üzerinden).

export interface User {
  id: string;
  fullName: string;
  email: string;
  roles: string[];
}

export interface LoginCredentials {
  email: string;
  password: string;
  twoFactorCode?: string;
}

/** Identity /auth/login yanıtı (AuthenticationResult). */
export interface AuthenticationResult {
  accessToken: string;
  refreshToken: string;
  userId: string;
  tenantId: string;
  email: string;
  role: string;
}

/** Identity /me yanıtı (CurrentUserDto). */
export interface CurrentUserDto {
  id: string;
  tenantId: string;
  email: string;
  fullName: string;
  role: string;
  twoFactorEnabled: boolean;
}

/** Sales /reports/daily. */
export interface DailySalesSummary {
  day: string;
  count: number;
  gross: number;
  commission: number;
  net: number;
}

/** Sales /reports/dashboard. */
export interface SalesDashboard {
  todayConsignmentCount: number;
  pendingSettlementTotal: number;
}

/** Finance /reports/aging (alt küme). */
export interface AgingReport {
  totalAmount: number;
  totalAccountCount: number;
}

/** Integration /reports/pending-documents. */
export interface PendingDocuments {
  pendingInvoices: number;
  pendingProducerReceipts: number;
  pendingHksNotifications: number;
  total: number;
}

/** Sayfalanmış liste yanıtı. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/** Sales SaleDto (üst düzey — liste). */
export interface Sale {
  id: string;
  buyerPartyId: string;
  producerPartyId: string;
  soldAt: string;
  grossAmount: number;
  status: number;
}

/** Finance CurrentAccountDto. */
export interface CurrentAccount {
  id: string;
  partyId: string;
  balance: number;
  entryCount: number;
}

/** ColdChain ColdStorageUnitDto (soğuk oda + son okuma özeti). */
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

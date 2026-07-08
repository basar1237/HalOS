// Dashboard okuma-modeli DTO'ları — backend rapor uçlarıyla birebir (camelCase JSON).
// Yalnız dashboard kartlarında kullanılan alanlar taşınır (tam DTO'nun alt kümesi olabilir).

/** GET /api/sales/reports/daily?day=... → Sales DailySummaryReportDto. */
export interface DailySalesSummary {
  day: string;
  count: number;
  gross: number;
  commission: number;
  net: number;
}

/** GET /api/finance/reports/aging?asOf=... → Finance CurrentAccountAgingReportDto (alt küme). */
export interface AgingBucket {
  amount: number;
  accountCount: number;
}

export interface AgingReport {
  asOfUtc: string;
  current: AgingBucket;
  days0To15: AgingBucket;
  days16To30: AgingBucket;
  days31Plus: AgingBucket;
  totalAmount: number;
  totalAccountCount: number;
}

/** GET /api/inventory/stock/low-stock → Inventory StockItemDto[] (yalnız sayısını kullanırız). */
export interface StockItem {
  id: string;
  productId: string;
  quantityOnHand: number;
  reorderThreshold: number | null;
}

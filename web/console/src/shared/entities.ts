// Liste sayfalarında kullanılan varlık DTO'ları — backend Application/Contracts ile birebir
// (camelCase JSON). Enum alanları INT (bkz. features/common/labels).

/** Party PartyDto — GET /api/party/parties. */
export interface WithholdingProfile {
  agriWithholdingRate: number;
  farmerSskRate: number;
}

export interface Party {
  id: string;
  tenantId: string;
  displayName: string;
  tckn: string | null;
  vkn: string | null;
  taxOffice: string | null;
  phone: string | null;
  address: string | null;
  keepsRecords: boolean;
  withholdingProfile: WithholdingProfile | null;
  isActive: boolean;
  createdOnUtc: string;
  roles: number[];
}

/** Finance CurrentAccountDto — GET /api/finance/current-accounts. */
export interface CurrentAccount {
  id: string;
  tenantId: string;
  partyId: string;
  balance: number;
  entryCount: number;
}

/** Inventory StockItemDto — GET /api/inventory/stock. */
export interface StockItem {
  id: string;
  tenantId: string;
  warehouseId: string;
  productId: string;
  quantityOnHand: number;
  reorderThreshold: number | null;
  movementCount: number;
}

/** Inventory ProductDto — GET /api/inventory/products. defaultUnit int (UnitOfMeasure). */
export interface Product {
  id: string;
  name: string;
  category: string | null;
  defaultUnit: number;
  isActive: boolean;
}

/** Sales SaleDto (üst düzey alanlar; liste için) — GET /api/sales/sales. */
export interface Sale {
  id: string;
  buyerPartyId: string;
  producerPartyId: string;
  soldAt: string;
  grossAmount: number;
  isWithinMarket: boolean;
  status: number;
  isCancelled: boolean;
}

/** Integration InvoiceDto — GET /api/integration/invoices. */
export interface Invoice {
  id: string;
  tenantId: string;
  saleTransactionId: string;
  buyerPartyId: string;
  issueDate: string;
  scenario: number;
  type: number;
  commissionAmount: number;
  commissionVatAmount: number;
  totalAmount: number;
  invoiceNumber: string | null;
  status: number;
}

/** Integration ProducerReceiptDto (e-MM) — GET /api/integration/producer-receipts. */
export interface ProducerReceipt {
  id: string;
  producerPartyId: string;
  issueDate: string;
  grossAmount: number;
  agriWithholdingAmount: number;
  farmerSskAmount: number;
  netPayable: number;
  receiptNumber: string | null;
  status: number;
}

/** Integration HksNotificationDto — GET /api/integration/hks-notifications. */
export interface HksNotification {
  id: string;
  producerPartyId: string;
  buyerPartyId: string;
  notifiedDate: string;
  grossAmount: number;
  commissionAmount: number;
  marketFeeAmount: number;
  referenceNumber: string | null;
  status: number;
}

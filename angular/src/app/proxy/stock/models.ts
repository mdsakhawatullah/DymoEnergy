export enum StockEntryType { StockIn = 1, StockOut = 2, Transfer = 3, Adjustment = 4 }
export enum StockEntryStatus { Draft = 1, Posted = 2, Reversed = 3 }
export enum StockOutReason { Sold = 1, Installed = 2, Damaged = 3, ReturnedToSupplier = 4, Lost = 5, InternalUse = 6, Other = 7 }

export const OUT_REASONS: { value: StockOutReason; label: string }[] = [
  { value: StockOutReason.Sold, label: 'Sold' },
  { value: StockOutReason.Installed, label: 'Used in installation' },
  { value: StockOutReason.Damaged, label: 'Damaged' },
  { value: StockOutReason.ReturnedToSupplier, label: 'Returned to supplier' },
  { value: StockOutReason.Lost, label: 'Lost or stolen' },
  { value: StockOutReason.InternalUse, label: 'Used internally' },
  { value: StockOutReason.Other, label: 'Other' },
];

export interface WarehouseDto {
  id: number;
  name: string;
  shortCode: string;
  address?: string | null;
  isDefault: boolean;
  isActive: boolean;
  order: number;
  units: number;
}

export interface CreateUpdateWarehouseDto {
  name: string;
  shortCode: string;
  address?: string | null;
  isDefault: boolean;
  isActive: boolean;
  order: number;
}

export interface StockSupplierDto {
  id: number;
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  note?: string | null;
  isActive: boolean;
}

export interface CreateUpdateStockSupplierDto {
  name: string;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  note?: string | null;
  isActive: boolean;
}

export interface StockOverviewDto {
  stockValue: number;
  stockUnits: number;
  productsInStock: number;
  productsWithoutCost: number;
  receivedUnits: number;
  receivedValue: number;
  receivedEntries: number;
  outUnits: number;
  outParts: { reason: StockOutReason; label: string; units: number }[];
  lowCount: number;
  restock: { productId: number; name: string; sku?: string | null; left: number }[];
  warehouses: WarehouseDto[];
  suppliers: StockSupplierDto[];
}

export interface GetStockEntriesInput {
  filter?: string;
  type?: StockEntryType;
  drafts?: boolean;
  status?: StockEntryStatus;
  warehouseId?: number;
  days?: number;
  skipCount: number;
  maxResultCount: number;
}

export interface StockEntryCountsDto { all: number; stockIn: number; stockOut: number; transfer: number; adjustment: number; drafts: number; }

export interface StockEntrySummaryDto {
  id: number;
  number: string;
  type: StockEntryType;
  status: StockEntryStatus;
  date: string;
  title: string;
  subtitle: string;
  itemCount: number;
  units: number;
  value: number;
  isReversal: boolean;
}

export interface StockEntriesPageDto { totalCount: number; items: StockEntrySummaryDto[]; counts: StockEntryCountsDto; }

export interface StockEntryLineDto {
  id: number;
  productId: number;
  productName: string;
  sku?: string | null;
  image?: string | null;
  quantity: number;
  countedQuantity?: number | null;
  unitCost: number;
  landedUnitCost: number;
  change: number;
  stockBefore?: number | null;
  stockAfter?: number | null;
  toStockAfter?: number | null;
  lineTotal: number;
  serials: string[];
  inStockNow: number;
  tracksSerials: boolean;
}

export interface StockAttachmentDto { id: number; fileName: string; url: string; sizeBytes: number; }

export interface StockEntryDto {
  id: number;
  number: string;
  type: StockEntryType;
  status: StockEntryStatus;
  date: string;
  warehouseId: number;
  warehouseName: string;
  toWarehouseId?: number | null;
  toWarehouseName?: string | null;
  supplierId?: number | null;
  supplierName?: string | null;
  invoiceNumber?: string | null;
  purchaseOrder?: string | null;
  transportCost: number;
  outReason?: StockOutReason | null;
  reference?: string | null;
  note?: string | null;
  postedAt?: string | null;
  postedByName?: string | null;
  reversesId?: number | null;
  reversesNumber?: string | null;
  reversedById?: number | null;
  reversedByNumber?: string | null;
  total: number;
  lines: StockEntryLineDto[];
  attachments: StockAttachmentDto[];
}

export interface SaveStockEntryLineDto {
  productId: number;
  quantity: number;
  countedQuantity?: number | null;
  unitCost: number;
  serials: string[];
}

export interface SaveStockEntryDto {
  type: StockEntryType;
  date: string;
  warehouseId: number;
  toWarehouseId?: number | null;
  supplierId?: number | null;
  invoiceNumber?: string | null;
  purchaseOrder?: string | null;
  transportCost: number;
  outReason?: StockOutReason | null;
  reference?: string | null;
  note?: string | null;
  lines: SaveStockEntryLineDto[];
}

export interface StockProductDto {
  id: number;
  name: string;
  sku?: string | null;
  image?: string | null;
  inStock: number;
  total: number;
  avgCost: number;
  lastCost?: number | null;
  tracksSerials: boolean;
}

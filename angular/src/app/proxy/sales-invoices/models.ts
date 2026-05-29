import { DymoPagedResultDto } from '../catalogues/models';

export { DymoPagedResultDto };

// ── Enums ─────────────────────────────────────────────────────────────────────

export type SalesInvoiceStatusType = 1 | 2 | 3 | 4 | 5 | 6 | 7;
export type SalesInvoicePaymentMethodType = 1 | 2 | 3 | 4 | 5;

export const SalesInvoiceStatusLabels: Record<number, string> = {
  1: 'Draft',
  2: 'Issued',
  3: 'Paid',
  4: 'Partially Paid',
  5: 'Overdue',
  6: 'Cancelled',
  7: 'Refunded',
};

export const SalesInvoiceStatusColors: Record<number, string> = {
  1: 'default',
  2: 'processing',
  3: 'success',
  4: 'warning',
  5: 'error',
  6: 'default',
  7: 'purple',
};

export const SalesInvoicePaymentMethodLabels: Record<number, string> = {
  1: 'Cash',
  2: 'Card',
  3: 'Bank Transfer',
  4: 'Cheque',
  5: 'Other',
};

// ── DTOs ─────────────────────────────────────────────────────────────────────

export interface SalesInvoiceDto {
  id: number;
  portalId?: number;
  invoiceNumber?: string;
  invoiceDate: string;
  dueDate?: string;
  customerId?: number;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
  billingAddress?: string;
  shippingAddress?: string;
  referenceNumber?: string;
  currencyCode: string;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  shippingCost: number;
  grandTotal: number;
  amountPaid: number;
  balanceDue: number;
  status: SalesInvoiceStatusType;
  paymentMethod?: SalesInvoicePaymentMethodType;
  paymentDate?: string;
  notes?: string;
  terms?: string;
  creationTime?: string;
  lastModificationTime?: string;
}

export interface SalesInvoiceItemDto {
  id: number;
  invoiceId: number;
  productId?: number;
  productName?: string;
  sku?: string;
  description?: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxRate: number;
  taxAmount: number;
  lineTotal: number;
  displayOrder: number;
}

export interface SalesInvoiceFilterDto {
  filter?: string;
  status?: SalesInvoiceStatusType;
  customerId?: number;
  portalId?: number;
  dateFrom?: string;
  dateTo?: string;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface CreateUpdateSalesInvoiceItemDto {
  id?: number;
  productId?: number;
  productName?: string;
  sku?: string;
  description?: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number;
  discountAmount: number;
  taxRate: number;
  taxAmount: number;
  lineTotal: number;
  displayOrder: number;
}

export interface CreateUpdateSalesInvoiceDto {
  portalId?: number;
  invoiceNumber?: string;
  invoiceDate: string;
  dueDate?: string;
  customerId?: number;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
  billingAddress?: string;
  shippingAddress?: string;
  referenceNumber?: string;
  currencyCode: string;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  shippingCost: number;
  grandTotal: number;
  amountPaid: number;
  balanceDue: number;
  status: SalesInvoiceStatusType;
  paymentMethod?: SalesInvoicePaymentMethodType;
  paymentDate?: string;
  notes?: string;
  terms?: string;
  items: CreateUpdateSalesInvoiceItemDto[];
}

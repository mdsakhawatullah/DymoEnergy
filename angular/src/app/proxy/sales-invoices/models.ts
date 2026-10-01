import { DymoPagedResultDto } from '../categories/models';

export { DymoPagedResultDto };

// ── Enums ─────────────────────────────────────────────────────────────────────

export type SalesInvoiceStatusType = 1 | 2 | 3 | 4 | 5 | 6 | 7;
export type SalesInvoicePaymentMethodType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8;
/** List tab filter — derived on the server from status, balance and due date. */
export type SalesInvoicePaymentStateType = 1 | 2 | 3;

export const SalesInvoicePaymentState = { Paid: 1, Due: 2, Overdue: 3 } as const;

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
  2: 'warning',
  3: 'success',
  4: 'warning',
  5: 'error',
  6: 'default',
  7: 'purple',
};

/** Collapsed status for list/drawer pills: Issued + Partially Paid both read as "Due". */
export const SalesInvoiceShortStatus: Record<number, { label: string; tone: 'green' | 'amber' | 'red' | 'gray' | 'purple' }> = {
  1: { label: 'Draft',     tone: 'gray'   },
  2: { label: 'Due',       tone: 'amber'  },
  3: { label: 'Paid',      tone: 'green'  },
  4: { label: 'Due',       tone: 'amber'  },
  5: { label: 'Overdue',   tone: 'red'    },
  6: { label: 'Cancelled', tone: 'gray'   },
  7: { label: 'Refunded',  tone: 'purple' },
};

export const SalesInvoicePaymentMethodLabels: Record<number, string> = {
  1: 'Cash',
  2: 'Card',
  3: 'Bank Transfer',
  4: 'Cheque',
  5: 'Other',
  6: 'bKash',
  7: 'Nagad',
  8: 'Rocket',
};

/** Display order for payment-method pickers (mobile money first — most common at the counter). */
export const SalesInvoicePaymentMethodOrder: SalesInvoicePaymentMethodType[] = [1, 6, 7, 8, 2, 3, 4, 5];

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
  channel?: string;
  subtotal: number;
  discountTotal: number;
  additionalDiscount: number;
  discountNote?: string;
  taxInclusive: boolean;
  taxTotal: number;
  shippingCost: number;
  grandTotal: number;
  amountPaid: number;
  balanceDue: number;
  /** Effective status — Overdue is derived server-side from the due date. */
  status: SalesInvoiceStatusType;
  paymentMethod?: SalesInvoicePaymentMethodType;
  paymentDate?: string;
  notes?: string;
  terms?: string;
  itemCount: number;
  firstItemName?: string;
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
  serialNumbers?: string;
  warranty?: string;
  displayOrder: number;
}

export interface SalesInvoicePaymentDto {
  id: number;
  invoiceId: number;
  amount: number;
  method: SalesInvoicePaymentMethodType;
  paidOn: string;
  referenceNumber?: string;
  note?: string;
  creationTime?: string;
}

export interface CollectSalesInvoicePaymentDto {
  amount: number;
  method: SalesInvoicePaymentMethodType;
  paidOn?: string;
  referenceNumber?: string;
  note?: string;
}

export interface SalesInvoiceFilterDto {
  filter?: string;
  status?: SalesInvoiceStatusType;
  paymentState?: SalesInvoicePaymentStateType;
  customerId?: number;
  portalId?: number;
  dateFrom?: string;
  dateTo?: string;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface SalesInvoiceSummaryInputDto {
  filter?: string;
  portalId?: number;
  dateFrom?: string;
  dateTo?: string;
}

export interface SalesInvoiceSummaryDto {
  salesTotal: number;
  collected: number;
  stillDue: number;
  allCount: number;
  paidCount: number;
  dueCount: number;
  overdueCount: number;
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
  serialNumbers?: string;
  warranty?: string;
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
  channel?: string;
  shippingCost: number;
  additionalDiscount: number;
  discountNote?: string;
  taxInclusive: boolean;
  /** Create only — recorded as the opening payment. */
  amountPaid: number;
  /** Draft, Issued, Cancelled or Refunded; the rest are derived. */
  status: SalesInvoiceStatusType;
  paymentMethod?: SalesInvoicePaymentMethodType;
  paymentDate?: string;
  notes?: string;
  terms?: string;
  items: CreateUpdateSalesInvoiceItemDto[];
}

import { DymoPagedResultDto } from '../catalogues/models';
export { DymoPagedResultDto };

// ── Enum label / colour maps ──────────────────────────────────────────────────

export const OrderStatusLabels: Record<number, string> = {
  1: 'Pending', 2: 'Confirmed', 3: 'Processing', 4: 'On Hold',
  5: 'Shipped',  6: 'Delivered', 7: 'Cancelled',  8: 'Refunded', 9: 'Returned',
};
export const OrderStatusColors: Record<number, string> = {
  1: 'gold',     2: 'processing', 3: 'blue',    4: 'orange',
  5: 'cyan',     6: 'success',    7: 'default',  8: 'purple',   9: 'magenta',
};

export const OrderStageLabels: Record<number, string> = {
  1: 'New',         2: 'Confirmed',    3: 'Processing', 4: 'Ready to Ship',
  5: 'Dispatched',  6: 'In Transit',   7: 'Delivered',  8: 'Completed', 9: 'Closed',
};
export const OrderStageColors: Record<number, string> = {
  1: 'default',   2: 'processing',  3: 'blue',     4: 'lime',
  5: 'cyan',      6: 'orange',      7: 'success',  8: 'green',  9: 'default',
};

export const OrderPriorityLabels: Record<number, string> = {
  1: 'Low', 2: 'Normal', 3: 'High', 4: 'Urgent',
};
export const OrderPriorityColors: Record<number, string> = {
  1: 'default', 2: 'processing', 3: 'orange', 4: 'error',
};

export const OrderShipmentTypeLabels: Record<number, string> = {
  1: 'Standard', 2: 'Express', 3: 'Overnight', 4: 'Pickup', 5: 'Local Delivery', 6: 'Freight',
};

export const OrderPaymentTypeLabels: Record<number, string> = {
  1: 'Cash', 2: 'Credit Card', 3: 'Debit Card', 4: 'Bank Transfer',
  5: 'Mobile Banking', 6: 'Cheque', 7: 'Online', 8: 'Cash on Delivery', 9: 'Other',
};

export const OrderCreateMethodLabels: Record<number, string> = {
  1: 'Web', 2: 'Phone', 3: 'Admin', 4: 'In Store', 5: 'API', 6: 'Marketplace',
};

// ── DTOs ─────────────────────────────────────────────────────────────────────

export interface OrderItemDto {
  id: number;
  orderId: number;
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

export interface OrderDto {
  id: number;
  portalId?: number;
  orderNumber?: string;
  orderDate: string;
  estimatedDeliveryDate?: string;
  actualDeliveryDate?: string;
  // Customer
  customerId?: number;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
  customerReference?: string;
  // Delivery
  billingAddress?: string;
  deliveryAddress?: string;
  deliveryContact?: string;
  deliveryPhone?: string;
  // Classification
  status: number;
  stage: number;
  priority: number;
  shipmentType: number;
  paymentType?: number;
  createMethod: number;
  createdHow?: string;
  // Voucher
  voucherCode?: string;
  voucherAmount: number;
  // Financials
  currencyCode: string;
  subtotal: number;
  discountTotal: number;
  taxRate: number;
  taxTotal: number;
  shippingCost: number;
  grandTotal: number;
  amountPaid: number;
  balanceDue: number;
  paymentDate?: string;
  // Notes
  notes?: string;
  notesInvoice?: string;
  terms?: string;
  internalNotes?: string;
  // Audit
  creationTime?: string;
  lastModificationTime?: string;
}

export interface OrderFilterDto {
  filter?: string;
  status?: number;
  stage?: number;
  priority?: number;
  paymentType?: number;
  paidState?: number;
  customerId?: number;
  portalId?: number;
  dateFrom?: string;
  dateTo?: string;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface CreateUpdateOrderItemDto {
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

export interface CreateUpdateOrderDto {
  portalId?: number;
  orderDate: string;
  estimatedDeliveryDate?: string;
  actualDeliveryDate?: string;
  customerId?: number;
  customerName?: string;
  customerEmail?: string;
  customerPhone?: string;
  customerReference?: string;
  billingAddress?: string;
  deliveryAddress?: string;
  deliveryContact?: string;
  deliveryPhone?: string;
  status: number;
  stage: number;
  priority: number;
  shipmentType: number;
  paymentType?: number;
  createMethod: number;
  createdHow?: string;
  voucherCode?: string;
  voucherAmount: number;
  currencyCode: string;
  subtotal: number;
  discountTotal: number;
  taxRate: number;
  taxTotal: number;
  shippingCost: number;
  grandTotal: number;
  amountPaid: number;
  balanceDue: number;
  paymentDate?: string;
  notes?: string;
  notesInvoice?: string;
  terms?: string;
  internalNotes?: string;
  items: CreateUpdateOrderItemDto[];
}

/** Settlement state derived from BalanceDue — mirrors DymoEnergy.Orders.OrderPaidState. */
export type OrderPaidStateType = 1 | 2 | 3;

export const OrderPaidStateLabels: Record<number, string> = {
  1: 'Paid in full',
  2: 'Partly paid',
  3: 'Unpaid',
};

export enum CourierProvider { Pathao = 1, Steadfast = 2, RedX = 3, ECourier = 4, OwnDelivery = 5, Pickup = 6 }
export enum CourierEnvironment { Live = 1, Sandbox = 2 }

export interface CourierFieldDto {
  key: string;
  label: string;
  help?: string;
  isSecret: boolean;
  hasValue: boolean;
  display?: string | null;
  changedAt?: string | null;
}

export interface CourierEnvironmentDto {
  environment: CourierEnvironment;
  keysSaved: boolean;
  fields: CourierFieldDto[];
}

export interface CourierSummaryDto {
  id: number;
  provider: CourierProvider;
  displayName: string;
  shortCode: string;
  color: string;
  isEnabled: boolean;
  order: number;
  activeEnvironment: CourierEnvironment;
  apiAvailable: boolean;
  isManual: boolean;
  status: 'connected' | 'keys' | 'not-connected' | 'always' | 'off';
  statusText: string;
}

export interface CourierLogDto {
  time: string;
  environment: CourierEnvironment;
  action: string;
  method?: string;
  endpoint?: string;
  statusCode?: number | null;
  durationMs?: number | null;
  result?: string;
  isError: boolean;
}

export interface CourierDetailDto extends CourierSummaryDto {
  environments: CourierEnvironmentDto[];
  pickupStoreId?: string | null;
  pickupStoreName?: string | null;
  defaultDeliveryType: number;
  defaultItemType: number;
  defaultWeightKg: number;
  codFeePercent: number;
  payoutSchedule?: string | null;
  tokenIssuedAt?: string | null;
  tokenExpiresAt?: string | null;
  webhookPath?: string | null;
  lastWebhookAt?: string | null;
  lastWebhookNote?: string | null;
  lastParcelAt?: string | null;
  logs: CourierLogDto[];
}

export interface CreateCourierDto { provider: CourierProvider; displayName: string; shortCode: string; color: string; }

export interface UpdateCourierSettingsDto {
  displayName: string;
  shortCode: string;
  color: string;
  isEnabled: boolean;
  pickupStoreId?: string | null;
  pickupStoreName?: string | null;
  defaultDeliveryType: number;
  defaultItemType: number;
  defaultWeightKg: number;
  codFeePercent: number;
  payoutSchedule?: string | null;
}

export interface CourierTestResultDto { ok: boolean; message: string; tokenExpiresAt?: string | null; }

export interface PathaoStoreDto { storeId: string; storeName: string; address?: string; isActive: boolean; isDefault: boolean; }

export interface ShippingOverviewDto {
  connectedCount: number;
  totalCount: number;
  connectedNames: string;
  anyLiveKeys: boolean;
  parcelsMoving: number;
  readyToSend: number;
  codDelivered: number;
  failedCallsToday: number;
  lastFailedCall?: string | null;
  couriers: CourierSummaryDto[];
}

export interface ReadyOrderDto {
  orderId: number;
  orderNumber: string;
  customerName: string;
  phone?: string;
  address?: string;
  items: string;
  weightKg: number;
  weightGuessed: boolean;
  collect: number;
  suggestedCourierId?: number | null;
  suggestedCourierName?: string | null;
  problems: string[];
  ruleNumber?: number | null;
  noParcel: boolean;
}

export interface SendParcelResultDto {
  orderId: number;
  orderNumber: string;
  ok: boolean;
  consignmentId?: string | null;
  deliveryFee?: number | null;
  message: string;
}

export interface ShipmentDto {
  id: number;
  orderId: number;
  orderNumber: string;
  consignmentId?: string | null;
  courierAccountId: number;
  courierName: string;
  courierColor: string;
  courierProvider: CourierProvider;
  environment: CourierEnvironment;
  status: string;
  stage: 'ready' | 'picked' | 'transit' | 'delivered' | 'failed' | 'returned' | 'cancelled';
  statusAt?: string | null;
  creationTime: string;
  recipientName: string;
  recipientAddress: string;
  codAmount: number;
  deliveryFee: number;
  deliveryType: number;
}

export interface ShipmentCountsDto { ready: number; pickedUp: number; inTransit: number; deliveredToday: number; failedOrReturning: number; }

export interface ShipmentsPageDto { ready: ReadyOrderDto[]; counts: ShipmentCountsDto; shipments: ShipmentDto[]; }

export interface PathaoLocationDto { id: number; name: string; homeDeliveryAvailable?: boolean | null; pickupAvailable?: boolean | null; }

export interface ShipmentEventDto {
  time: string;
  status: string;
  stage: ShipmentDto['stage'];
  source: 'sent' | 'webhook' | 'tracked' | 'now';
  event?: string | null;
  note?: string | null;
  collectedAmount?: number | null;
}

export interface ShipmentOrderDto {
  id: number;
  number: string;
  date: string;
  status: string;
  paymentType?: string | null;
  customerName?: string | null;
  customerPhone?: string | null;
  customerEmail?: string | null;
  deliveryContact?: string | null;
  deliveryPhone?: string | null;
  deliveryAddress?: string | null;
  notes?: string | null;
  subtotal: number;
  discountTotal: number;
  taxTotal: number;
  shippingCost: number;
  grandTotal: number;
  amountPaid: number;
  balanceDue: number;
  items: { name: string; sku?: string | null; quantity: number; unitPrice: number; lineTotal: number }[];
}

export interface ShipmentDetailDto {
  shipment: ShipmentDto;
  merchantOrderId?: string | null;
  recipientPhone: string;
  weightKg: number;
  itemType: number;
  note?: string | null;
  courierShortCode: string;
  canTrack: boolean;
  payoutNote?: string | null;
  events: ShipmentEventDto[];
  order?: ShipmentOrderDto | null;
}

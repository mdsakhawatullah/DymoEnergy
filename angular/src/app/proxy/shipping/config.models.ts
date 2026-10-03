import type { CourierSummaryDto } from './models';

export enum ShippingItemKind { BigItem = 1, ReturnPolicy = 2, PackingRule = 3, CustomerMessage = 4 }

// ── Charges & zones ─────────────────────────────────────────────────────────

export interface ShippingSettingDto {
  freeDeliveryEnabled: boolean;
  freeDeliveryOver: number;
  weightChargeEnabled: boolean;
  weightIncludedKg: number;
  codFeePassed: boolean;
  codFeePercent: number;
  customerChoosesCourier: boolean;
  ownTruckPerKm: number;
  minTripCharge: number;
  noCashAlertDays: number;
}

export interface ShippingZoneDto {
  id: number;
  name: string;
  note?: string | null;
  charge: number;
  perExtraKg: number;
  courierCost: number;
  days?: string | null;
  order: number;
  margin: number;
}

export interface CreateUpdateShippingZoneDto {
  name: string;
  note?: string | null;
  charge: number;
  perExtraKg: number;
  courierCost: number;
  days?: string | null;
  order: number;
}

export interface ShippingItemDto {
  id: number;
  kind: ShippingItemKind;
  title: string;
  detail?: string | null;
  extra?: string | null;
  color?: string | null;
  flag: boolean;
  order: number;
}

export interface CreateUpdateShippingItemDto {
  kind: ShippingItemKind;
  title: string;
  detail?: string | null;
  extra?: string | null;
  color?: string | null;
  flag: boolean;
  order: number;
}

export interface ChargesPageDto {
  setting: ShippingSettingDto;
  zones: ShippingZoneDto[];
  bigItems: ShippingItemDto[];
  returnPolicies: ShippingItemDto[];
}

// ── Rules & packaging ───────────────────────────────────────────────────────

export interface CreateUpdateCourierRuleDto {
  order: number;
  isEnabled: boolean;
  matchAny: boolean;
  productKeyword?: string | null;
  anyItemOverKg?: number | null;
  totalWeightUnderKg?: number | null;
  addressContains?: string | null;
  codOver?: number | null;
  needsInstallation: boolean;
  courierAccountId?: number | null;
  noParcel: boolean;
  thenNote?: string | null;
}

export interface CourierRuleDto extends CreateUpdateCourierRuleDto {
  id: number;
  ifText: string;
  thenText: string;
  courierColor?: string | null;
  matchCount: number;
}

export interface CourierPerformanceDto {
  courierAccountId: number;
  name: string;
  color: string;
  parcels: number;
  successPercent?: number | null;
  averageDays?: number | null;
}

export interface RulesPageDto {
  rules: CourierRuleDto[];
  packingRules: ShippingItemDto[];
  customerMessages: ShippingItemDto[];
  performance: CourierPerformanceDto[];
  couriers: CourierSummaryDto[];
  ordersChecked: number;
  unmatched: number;
}

// ── Cash on delivery ────────────────────────────────────────────────────────

export interface CodCourierDto {
  courierAccountId: number;
  name: string;
  shortCode: string;
  color: string;
  parcels: number;
  collected: number;
  fee: number;
  shouldReceive: number;
  schedule?: string | null;
  codFeePercent: number;
}

export interface CodParcelDto {
  shipmentId: number;
  courierAccountId: number;
  consignmentId?: string | null;
  orderNumber: string;
  deliveredAt: string;
  cod: number;
  fee: number;
  expected: number;
}

export interface CodPayoutDto {
  id: number;
  courierAccountId: number;
  courierName: string;
  date: string;
  amount: number;
  expected: number;
  difference: number;
  parcels: number;
  status: 'matched' | 'short' | 'over';
  reference?: string | null;
  note?: string | null;
  banked: boolean;
}

export interface CodIssueDto {
  tone: 'red' | 'amber' | 'grey';
  title: string;
  text: string;
  action: 'payout' | 'view-payout';
  actionLabel: string;
  courierAccountId?: number | null;
  payoutId?: number | null;
}

export interface CodPageDto {
  holding: number;
  holdingParcels: number;
  oldestUnpaidDays?: number | null;
  receivedThisMonth: number;
  receivedCount: number;
  matchedCount: number;
  shortTotal: number;
  shortCount: number;
  couriers: CodCourierDto[];
  payouts: CodPayoutDto[];
  issues: CodIssueDto[];
  unpaid: CodParcelDto[];
  financeAccounts: { id: number; name: string }[];
}

export interface CreateCourierPayoutDto {
  courierAccountId: number;
  date: string;
  amount: number;
  reference?: string | null;
  note?: string | null;
  shipmentIds: number[];
  financeAccountId?: number | null;
}

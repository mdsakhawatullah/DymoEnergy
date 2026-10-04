import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  ChargesPageDto,
  CodPageDto,
  CodPayoutDto,
  CourierRuleDto,
  CreateCourierPayoutDto,
  CreateUpdateCourierRuleDto,
  CreateUpdateShippingItemDto,
  CreateUpdateShippingZoneDto,
  RefreshZonePricesResultDto,
  RulesPageDto,
  ShippingItemDto,
  ShippingSettingDto,
  ShippingZoneDto,
} from './config.models';

const BASE = '/api/app/shipping-config';

/** Charges & zones, cash on delivery, and rules & packaging. */
@Injectable({ providedIn: 'root' })
export class ShippingConfigService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  // ── Charges & zones ─────────────────────────────────────────────────────
  getCharges = () => this.req<ChargesPageDto>({ method: 'GET', url: `${BASE}/charges` });
  updateSetting = (input: ShippingSettingDto) => this.req<ShippingSettingDto>({ method: 'PUT', url: `${BASE}/setting`, body: input });
  createZone = (input: CreateUpdateShippingZoneDto) => this.req<ShippingZoneDto>({ method: 'POST', url: `${BASE}/zone`, body: input });
  updateZone = (id: number, input: CreateUpdateShippingZoneDto) => this.req<ShippingZoneDto>({ method: 'PUT', url: `${BASE}/${id}/zone`, body: input });
  deleteZone = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/zone` });
  /** Asks Pathao's price plan for one zone, or for every linked zone when no id is given. */
  refreshZonePrices = (zoneId?: number) =>
    this.req<RefreshZonePricesResultDto>({ method: 'POST', url: `${BASE}/refresh-zone-prices`, body: { zoneId: zoneId ?? null } });

  // ── Lists ───────────────────────────────────────────────────────────────
  createItem = (input: CreateUpdateShippingItemDto) => this.req<ShippingItemDto>({ method: 'POST', url: `${BASE}/item`, body: input });
  updateItem = (id: number, input: CreateUpdateShippingItemDto) => this.req<ShippingItemDto>({ method: 'PUT', url: `${BASE}/${id}/item`, body: input });
  deleteItem = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/item` });

  // ── Rules ───────────────────────────────────────────────────────────────
  getRules = () => this.req<RulesPageDto>({ method: 'GET', url: `${BASE}/rules` });
  createRule = (input: CreateUpdateCourierRuleDto) => this.req<CourierRuleDto>({ method: 'POST', url: `${BASE}/rule`, body: input });
  updateRule = (id: number, input: CreateUpdateCourierRuleDto) => this.req<CourierRuleDto>({ method: 'PUT', url: `${BASE}/${id}/rule`, body: input });
  deleteRule = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/rule` });

  // ── Cash on delivery ────────────────────────────────────────────────────
  getCod = () => this.req<CodPageDto>({ method: 'GET', url: `${BASE}/cod` });
  createPayout = (input: CreateCourierPayoutDto) => this.req<CodPayoutDto>({ method: 'POST', url: `${BASE}/payout`, body: input });
  deletePayout = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/payout` });
}

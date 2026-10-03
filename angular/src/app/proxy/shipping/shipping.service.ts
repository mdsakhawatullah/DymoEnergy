import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import { map } from 'rxjs';
import type {
  CourierDetailDto,
  CourierEnvironment,
  CourierTestResultDto,
  CreateCourierDto,
  PathaoStoreDto,
  SendParcelResultDto,
  ShipmentDto,
  ShipmentsPageDto,
  ShippingOverviewDto,
  UpdateCourierSettingsDto,
} from './models';

const BASE = '/api/app/shipping';

@Injectable({ providedIn: 'root' })
export class ShippingService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any; responseType?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  getOverview = () => this.req<ShippingOverviewDto>({ method: 'GET', url: `${BASE}/overview` });

  // ── Couriers & keys ─────────────────────────────────────────────────────
  getCourier = (id: number) => this.req<CourierDetailDto>({ method: 'GET', url: `${BASE}/${id}/courier` });
  createCourier = (input: CreateCourierDto) => this.req<CourierDetailDto>({ method: 'POST', url: `${BASE}/courier`, body: input });
  updateSettings = (id: number, input: UpdateCourierSettingsDto) =>
    this.req<CourierDetailDto>({ method: 'PUT', url: `${BASE}/${id}/courier-settings`, body: input });
  updateCredential = (id: number, environment: CourierEnvironment, key: string, value: string | null) =>
    this.req<CourierDetailDto>({ method: 'PUT', url: `${BASE}/${id}/courier-credential`, body: { environment, key, value } });
  /** Returns the secret in full; the server writes a "Key revealed" entry to the call log. */
  reveal = (id: number, environment: CourierEnvironment, key: string) =>
    this.req<string>({ method: 'GET', url: `${BASE}/${id}/courier-credential-reveal`, params: { environment, key }, responseType: 'text' }).pipe(
      map(v => (typeof v === 'string' && v.startsWith('"') ? JSON.parse(v) : v) as string),
    );
  setEnvironment = (id: number, environment: CourierEnvironment) =>
    this.req<CourierDetailDto>({ method: 'PUT', url: `${BASE}/${id}/courier-environment`, body: { environment } });
  test = (id: number) => this.req<CourierTestResultDto>({ method: 'POST', url: `${BASE}/${id}/test-courier` });
  newToken = (id: number) => this.req<CourierTestResultDto>({ method: 'POST', url: `${BASE}/${id}/refresh-courier-token` });
  testAll = () => this.req<CourierTestResultDto[]>({ method: 'POST', url: `${BASE}/test-all-couriers` });
  disconnect = (id: number) => this.req<CourierDetailDto>({ method: 'POST', url: `${BASE}/${id}/disconnect-courier` });

  // ── Pathao ──────────────────────────────────────────────────────────────
  getPathaoStores = (id: number) => this.req<PathaoStoreDto[]>({ method: 'GET', url: `${BASE}/${id}/pathao-stores` });

  // ── Shipments ───────────────────────────────────────────────────────────
  getShipments = (filter?: string) => this.req<ShipmentsPageDto>({ method: 'GET', url: `${BASE}/shipments`, params: { filter } });
  sendParcels = (orderIds: number[], courierAccountId: number) =>
    this.req<SendParcelResultDto[]>({ method: 'POST', url: `${BASE}/send-parcels`, body: { orderIds, courierAccountId } });
  refreshShipment = (id: number) => this.req<ShipmentDto>({ method: 'POST', url: `${BASE}/${id}/refresh-shipment` });
}

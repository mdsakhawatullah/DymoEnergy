import { RestService } from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  CreateUpdateStockSupplierDto,
  CreateUpdateWarehouseDto,
  GetStockEntriesInput,
  SaveStockEntryDto,
  StockAttachmentDto,
  StockEntriesPageDto,
  StockEntryDto,
  StockOverviewDto,
  StockProductDto,
  StockSupplierDto,
  WarehouseDto,
} from './models';

const BASE = '/api/app/stock-entry';

@Injectable({ providedIn: 'root' })
export class StockService {
  apiName = 'Default';
  private readonly filesUrl = `${environment.apis['default'].url}/api/app/stock-files`;

  constructor(private restService: RestService, private http: HttpClient) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any; responseType?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  getOverview = () => this.req<StockOverviewDto>({ method: 'GET', url: `${BASE}/overview` });
  getEntries = (input: GetStockEntriesInput) => this.req<StockEntriesPageDto>({ method: 'GET', url: `${BASE}/entries`, params: input });
  get = (id: number) => this.req<StockEntryDto>({ method: 'GET', url: `${BASE}/${id}` });
  nextNumber = () =>
    this.req<string>({ method: 'GET', url: `${BASE}/next-number`, responseType: 'text' }).pipe(
      map(v => (typeof v === 'string' && v.startsWith('"') ? JSON.parse(v) : v) as string),
    );

  create = (input: SaveStockEntryDto) => this.req<StockEntryDto>({ method: 'POST', url: BASE, body: input });
  update = (id: number, input: SaveStockEntryDto) => this.req<StockEntryDto>({ method: 'PUT', url: `${BASE}/${id}`, body: input });
  delete = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}` });
  post = (id: number) => this.req<StockEntryDto>({ method: 'POST', url: `${BASE}/${id}` });
  reverse = (id: number) => this.req<StockEntryDto>({ method: 'POST', url: `${BASE}/${id}/reverse` });

  addAttachment = (id: number, input: { fileName: string; url: string; sizeBytes: number }) =>
    this.req<StockAttachmentDto>({ method: 'POST', url: `${BASE}/${id}/attachment`, body: input });
  deleteAttachment = (attachmentId: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/attachment/${attachmentId}` });

  getProducts = (input: { filter?: string; warehouseId?: number; ids?: number[]; maxResultCount?: number }) =>
    this.req<StockProductDto[]>({ method: 'GET', url: `${BASE}/products`, params: input });

  createWarehouse = (input: CreateUpdateWarehouseDto) => this.req<WarehouseDto>({ method: 'POST', url: `${BASE}/warehouse`, body: input });
  updateWarehouse = (id: number, input: CreateUpdateWarehouseDto) => this.req<WarehouseDto>({ method: 'PUT', url: `${BASE}/${id}/warehouse`, body: input });
  createSupplier = (input: CreateUpdateStockSupplierDto) => this.req<StockSupplierDto>({ method: 'POST', url: `${BASE}/supplier`, body: input });
  updateSupplier = (id: number, input: CreateUpdateStockSupplierDto) => this.req<StockSupplierDto>({ method: 'PUT', url: `${BASE}/${id}/supplier`, body: input });

  upload(file: File): Observable<{ url: string; fileName: string; sizeBytes: number }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string; fileName: string; sizeBytes: number }>(`${this.filesUrl}/upload`, form);
  }
}

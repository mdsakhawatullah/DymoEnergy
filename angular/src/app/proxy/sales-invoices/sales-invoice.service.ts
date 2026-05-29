import type {
  CreateUpdateSalesInvoiceDto,
  SalesInvoiceDto,
  SalesInvoiceFilterDto,
  SalesInvoiceItemDto,
} from './models';
import { DymoPagedResultDto } from '../catalogues/models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class SalesInvoiceService {
  apiName = 'Default';

  getListData = (input: SalesInvoiceFilterDto) =>
    this.restService.request<any, DymoPagedResultDto<SalesInvoiceDto>>(
      {
        method: 'GET',
        url: '/api/app/sales-invoice/data',
        params: {
          filter:         input.filter,
          status:         input.status,
          customerId:     input.customerId,
          portalId:       input.portalId,
          dateFrom:       input.dateFrom,
          dateTo:         input.dateTo,
          sorting:        input.sorting,
          skipCount:      input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, SalesInvoiceDto>(
      { method: 'GET', url: `/api/app/sales-invoice/${id}` },
      { apiName: this.apiName }
    );

  // ABP maps GetItemsAsync(int invoiceId) → GET /items?invoiceId=…  (not /{id}/items)
  getItems = (invoiceId: number) =>
    this.restService.request<any, SalesInvoiceItemDto[]>(
      { method: 'GET', url: `/api/app/sales-invoice/sale-invoice-item/${invoiceId}`},
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateSalesInvoiceDto) =>
    this.restService.request<any, SalesInvoiceDto>(
      { method: 'POST', url: '/api/app/sales-invoice/invoice-data', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateSalesInvoiceDto) =>
    this.restService.request<any, SalesInvoiceDto>(
      { method: 'PUT', url: `/api/app/sales-invoice/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/sales-invoice/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

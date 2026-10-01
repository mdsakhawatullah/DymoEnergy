import type {
  CollectSalesInvoicePaymentDto,
  CreateUpdateSalesInvoiceDto,
  SalesInvoiceDto,
  SalesInvoiceFilterDto,
  SalesInvoiceItemDto,
  SalesInvoicePaymentDto,
  SalesInvoiceSummaryDto,
  SalesInvoiceSummaryInputDto,
} from './models';
import { DymoPagedResultDto } from '../categories/models';
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
          paymentState:   input.paymentState,
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

  getSummary = (input: SalesInvoiceSummaryInputDto) =>
    this.restService.request<any, SalesInvoiceSummaryDto>(
      {
        method: 'GET',
        url: '/api/app/sales-invoice/summary',
        params: {
          filter:   input.filter,
          portalId: input.portalId,
          dateFrom: input.dateFrom,
          dateTo:   input.dateTo,
        },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, SalesInvoiceDto>(
      { method: 'GET', url: `/api/app/sales-invoice/${id}` },
      { apiName: this.apiName }
    );

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

  // ── Payments ────────────────────────────────────────────────────────────────

  getPayments = (id: number) =>
    this.restService.request<any, SalesInvoicePaymentDto[]>(
      { method: 'GET', url: `/api/app/sales-invoice/${id}/payments` },
      { apiName: this.apiName }
    );

  collectPayment = (id: number, input: CollectSalesInvoicePaymentDto) =>
    this.restService.request<any, SalesInvoiceDto>(
      { method: 'POST', url: `/api/app/sales-invoice/${id}/collect-payment`, body: input },
      { apiName: this.apiName }
    );

  deletePayment = (id: number, paymentId: number) =>
    this.restService.request<any, SalesInvoiceDto>(
      { method: 'DELETE', url: `/api/app/sales-invoice/${id}/payment/${paymentId}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

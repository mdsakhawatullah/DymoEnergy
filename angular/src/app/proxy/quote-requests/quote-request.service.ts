import type {
  QuoteRequestDto,
  QuoteRequestFilterDto,
  QuoteRequestSummaryDto,
  UpdateQuoteRequestDetailsDto,
  UpdateQuoteRequestStatusDto,
} from './models';
import { DymoPagedResultDto } from '../categories/models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class QuoteRequestService {
  apiName = 'Default';

  getListData = (input: QuoteRequestFilterDto) =>
    this.restService.request<any, DymoPagedResultDto<QuoteRequestDto>>(
      {
        method: 'GET',
        url: '/api/app/quote-request/data',
        params: {
          filter:         input.filter,
          status:         input.status,
          interest:       input.interest,
          sorting:        input.sorting,
          skipCount:      input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName }
    );

  getSummary = (input: QuoteRequestFilterDto) =>
    this.restService.request<any, QuoteRequestSummaryDto>(
      {
        method: 'GET',
        url: '/api/app/quote-request/summary',
        params: { filter: input.filter, interest: input.interest },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, QuoteRequestDto>(
      { method: 'GET', url: `/api/app/quote-request/${id}` },
      { apiName: this.apiName }
    );

  updateStatus = (id: number, input: UpdateQuoteRequestStatusDto) =>
    this.restService.request<any, QuoteRequestDto>(
      { method: 'PUT', url: `/api/app/quote-request/${id}/status`, body: input },
      { apiName: this.apiName }
    );

  updateDetails = (id: number, input: UpdateQuoteRequestDetailsDto) =>
    this.restService.request<any, QuoteRequestDto>(
      { method: 'PUT', url: `/api/app/quote-request/${id}/details`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/quote-request/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

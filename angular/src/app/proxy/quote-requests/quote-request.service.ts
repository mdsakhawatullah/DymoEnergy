import type { QuoteRequestDto, QuoteRequestFilterDto, UpdateQuoteRequestStatusDto } from './models';
import { DymoPagedResultDto } from '../catalogues/models';
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
          sorting:        input.sorting,
          skipCount:      input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName }
    );

  updateStatus = (id: number, input: UpdateQuoteRequestStatusDto) =>
    this.restService.request<any, QuoteRequestDto>(
      { method: 'PUT', url: `/api/app/quote-request/${id}/status`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/quote-request/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

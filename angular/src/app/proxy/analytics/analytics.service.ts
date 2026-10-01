import type {
  AnalyticsCustomersDto,
  AnalyticsFilterDto,
  AnalyticsMoneyDto,
  AnalyticsOverviewDto,
  AnalyticsProductsDto,
  AnalyticsReportDto,
  AnalyticsSalesDto,
} from './models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class AnalyticsService {
  apiName = 'Default';

  private params(input: AnalyticsFilterDto) {
    return { dateFrom: input.dateFrom, dateTo: input.dateTo, channel: input.channel };
  }

  private getTab<T>(tab: string, input: AnalyticsFilterDto) {
    return this.restService.request<any, T>(
      { method: 'GET', url: `/api/app/analytics/${tab}`, params: this.params(input) },
      { apiName: this.apiName }
    );
  }

  getChannels = () =>
    this.restService.request<any, string[]>(
      { method: 'GET', url: '/api/app/analytics/channels' },
      { apiName: this.apiName }
    );

  getOverview  = (input: AnalyticsFilterDto) => this.getTab<AnalyticsOverviewDto>('overview', input);
  getSales     = (input: AnalyticsFilterDto) => this.getTab<AnalyticsSalesDto>('sales', input);
  getProducts  = (input: AnalyticsFilterDto) => this.getTab<AnalyticsProductsDto>('products', input);
  getMoney     = (input: AnalyticsFilterDto) => this.getTab<AnalyticsMoneyDto>('money', input);
  getCustomers = (input: AnalyticsFilterDto) => this.getTab<AnalyticsCustomersDto>('customers', input);

  getReport = (key: string, input: AnalyticsFilterDto) =>
    this.restService.request<any, AnalyticsReportDto>(
      { method: 'GET', url: '/api/app/analytics/report', params: { key, ...this.params(input) } },
      { apiName: this.apiName }
    );

  setSalesTarget = (monthlyTarget: number) =>
    this.restService.request<any, void>(
      { method: 'POST', url: '/api/app/analytics/set-sales-target', body: { monthlyTarget } },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

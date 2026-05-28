import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  AdminSiteSettingDto,
  CreateUpdateAdminSiteSettingDto,
  PagedAdminSiteSettingResultDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class AdminSiteSettingService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  getList = (params: { skipCount?: number; maxResultCount?: number } = {}) =>
    this.restService.request<any, PagedAdminSiteSettingResultDto>(
      {
        method: 'GET',
        url: '/api/app/admin-site-setting',
        params: { skipCount: params.skipCount ?? 0, maxResultCount: params.maxResultCount ?? 10 },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, AdminSiteSettingDto>(
      { method: 'GET', url: `/api/app/admin-site-setting/${id}` },
      { apiName: this.apiName }
    );

  getActive = () =>
    this.restService.request<any, AdminSiteSettingDto>(
      { method: 'GET', url: '/api/app/admin-site-setting/active' },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateAdminSiteSettingDto) =>
    this.restService.request<any, AdminSiteSettingDto>(
      { method: 'POST', url: '/api/app/admin-site-setting', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateAdminSiteSettingDto) =>
    this.restService.request<any, AdminSiteSettingDto>(
      { method: 'PUT', url: `/api/app/admin-site-setting/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/admin-site-setting/${id}` },
      { apiName: this.apiName }
    );
}

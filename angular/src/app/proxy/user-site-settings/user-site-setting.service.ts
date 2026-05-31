import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  CreateUpdateUserSiteSettingDto,
  PagedUserSiteSettingResultDto,
  UserSiteSettingDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class UserSiteSettingService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  getList = (params: { skipCount?: number; maxResultCount?: number } = {}) =>
    this.restService.request<any, PagedUserSiteSettingResultDto>(
      {
        method: 'GET',
        url: '/api/app/user-site-setting',
        params: { skipCount: params.skipCount ?? 0, maxResultCount: params.maxResultCount ?? 10 },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, UserSiteSettingDto>(
      { method: 'GET', url: `/api/app/user-site-setting/${id}` },
      { apiName: this.apiName }
    );

  getActive = () =>
    this.restService.request<any, UserSiteSettingDto>(
      { method: 'GET', url: '/api/app/user-site-setting/active' },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateUserSiteSettingDto) =>
    this.restService.request<any, UserSiteSettingDto>(
      { method: 'POST', url: '/api/app/user-site-setting', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateUserSiteSettingDto) =>
    this.restService.request<any, UserSiteSettingDto>(
      { method: 'PUT', url: `/api/app/user-site-setting/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/user-site-setting/${id}` },
      { apiName: this.apiName }
    );

  setActive = (id: number) =>
    this.restService.request<any, UserSiteSettingDto>(
      { method: 'POST', url: `/api/app/user-site-setting/${id}/set-active` },
      { apiName: this.apiName }
    );
}

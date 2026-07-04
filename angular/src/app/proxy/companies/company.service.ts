import type { CompanyDto, CompanyFilterDto, CreateUpdateCompanyDto } from './models';
import { DymoPagedResultDto } from '../catalogues/models';
import { SelectListDto } from '../catalogues/models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CompanyService {
  apiName = 'Default';

  getListData = (input: CompanyFilterDto) =>
    this.restService.request<any, DymoPagedResultDto<CompanyDto>>(
      {
        method: 'GET',
        url: '/api/app/company/data',
        params: {
          filter:          input.filter,
          status:          input.status,
          isParentCompany: input.isParentCompany,
          parentCompanyId: input.parentCompanyId,
          sorting:         input.sorting,
          skipCount:       input.skipCount,
          maxResultCount:  input.maxResultCount,
        },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, CompanyDto>(
      { method: 'GET', url: `/api/app/company/${id}` },
      { apiName: this.apiName }
    );

  getSelectList = () =>
    this.restService.request<any, SelectListDto[]>(
      { method: 'GET', url: '/api/app/company/select-list' },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateCompanyDto) =>
    this.restService.request<any, CompanyDto>(
      { method: 'POST', url: '/api/app/company/company-data', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateCompanyDto) =>
    this.restService.request<any, CompanyDto>(
      { method: 'PUT', url: `/api/app/company/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/company/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

import type { CatalogueDto, CatalogueFilterDto, CreateUpdateCatalogueDto, DymoPagedResultDto } from './models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CatalogueService {
  apiName = 'Default';

  getListData = (input: CatalogueFilterDto) =>
    this.restService.request<any, DymoPagedResultDto<CatalogueDto>>(
      {
        method: 'GET',
        url: '/api/app/catalogue/data',
        params: {
          filter: input.filter,
          isPublished: input.isPublished,
          isFeatured: input.isFeatured,
          portalId: input.portalId,
          sorting: input.sorting,
          skipCount: input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, CatalogueDto>(
      { method: 'GET', url: `/api/app/catalogue/${id}` },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateCatalogueDto) =>
    this.restService.request<any, CatalogueDto>(
      { method: 'POST', url: '/api/app/catalogue/catalogue-data', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateCatalogueDto) =>
    this.restService.request<any, CatalogueDto>(
      { method: 'PUT', url: `/api/app/catalogue/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/catalogue/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

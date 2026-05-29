import type { CreateUpdateProductDto, ProductDto, ProductFilterDto } from './models';
import { DymoPagedResultDto } from '../catalogues/models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ProductService {
  apiName = 'Default';

  getListData = (input: ProductFilterDto) =>
    this.restService.request<any, DymoPagedResultDto<ProductDto>>(
      {
        method: 'GET',
        url: '/api/app/product/data',
        params: {
          filter:         input.filter,
          status:         input.status,
          isActive:       input.isActive,
          isFeatured:     input.isFeatured,
          catalogueId:    input.catalogueId,
          portalId:       input.portalId,
          sorting:        input.sorting,
          skipCount:      input.skipCount,
          maxResultCount: input.maxResultCount,
        },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, ProductDto>(
      { method: 'GET', url: `/api/app/product/${id}` },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateProductDto) =>
    this.restService.request<any, ProductDto>(
      { method: 'POST', url: '/api/app/product/product-data', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateProductDto) =>
    this.restService.request<any, ProductDto>(
      { method: 'PUT', url: `/api/app/product/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/product/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

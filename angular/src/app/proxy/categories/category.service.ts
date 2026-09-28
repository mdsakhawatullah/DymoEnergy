import type { CategoryDto, CategoryFilterDto, CreateUpdateCategoryDto, DymoPagedResultDto, SelectListDto } from './models';
import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  apiName = 'Default';

  getListData = (input: CategoryFilterDto) =>
    this.restService.request<any, DymoPagedResultDto<CategoryDto>>(
      {
        method: 'GET',
        url: '/api/app/category/data',
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

  getSelectList = () =>
    this.restService.request<any, SelectListDto[]>(
      { method: 'GET', url: '/api/app/category/select-list' },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, CategoryDto>(
      { method: 'GET', url: `/api/app/category/${id}` },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateCategoryDto) =>
    this.restService.request<any, CategoryDto>(
      { method: 'POST', url: '/api/app/category/category-data', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateCategoryDto) =>
    this.restService.request<any, CategoryDto>(
      { method: 'PUT', url: `/api/app/category/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/category/${id}` },
      { apiName: this.apiName }
    );

  constructor(private restService: RestService) {}
}

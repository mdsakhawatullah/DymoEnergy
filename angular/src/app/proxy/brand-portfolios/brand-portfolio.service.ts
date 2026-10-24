import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  BrandPortfolioDto,
  CreateUpdateBrandPortfolioDto,
  ListBrandPortfolioResultDto,
  PagedBrandPortfolioResultDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class BrandPortfolioService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  getList = (
    params: {
      filter?: string;
      isPublished?: boolean;
      skipCount?: number;
      maxResultCount?: number;
    } = {}
  ) =>
    this.restService.request<any, PagedBrandPortfolioResultDto>(
      {
        method: 'GET',
        url: '/api/app/brand-portfolio',
        params: {
          filter: params.filter,
          isPublished: params.isPublished,
          skipCount: params.skipCount ?? 0,
          maxResultCount: params.maxResultCount ?? 20,
        },
      },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, BrandPortfolioDto>(
      { method: 'GET', url: `/api/app/brand-portfolio/${id}` },
      { apiName: this.apiName }
    );

  /** Storefront lookup — published pages only. */
  getBySlug = (slug: string) =>
    this.restService.request<any, BrandPortfolioDto>(
      { method: 'GET', url: '/api/app/brand-portfolio/by-slug', params: { slug } },
      { apiName: this.apiName }
    );

  getPublished = () =>
    this.restService.request<any, ListBrandPortfolioResultDto>(
      { method: 'GET', url: '/api/app/brand-portfolio/published' },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateBrandPortfolioDto) =>
    this.restService.request<any, BrandPortfolioDto>(
      { method: 'POST', url: '/api/app/brand-portfolio', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateBrandPortfolioDto) =>
    this.restService.request<any, BrandPortfolioDto>(
      { method: 'PUT', url: `/api/app/brand-portfolio/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/brand-portfolio/${id}` },
      { apiName: this.apiName }
    );

  setPublished = (id: number, isPublished: boolean) =>
    this.restService.request<any, BrandPortfolioDto>(
      {
        method: 'POST',
        url: `/api/app/brand-portfolio/${id}/set-published`,
        params: { isPublished },
      },
      { apiName: this.apiName }
    );
}

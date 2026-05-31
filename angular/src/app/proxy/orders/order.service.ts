import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  CreateUpdateOrderDto,
  DymoPagedResultDto,
  OrderDto,
  OrderFilterDto,
  OrderItemDto,
} from './models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  getListData = (params: OrderFilterDto = {}) =>
    this.restService.request<any, DymoPagedResultDto<OrderDto>>(
      { method: 'GET', url: '/api/app/order/data', params },
      { apiName: this.apiName }
    );

  get = (id: number) =>
    this.restService.request<any, OrderDto>(
      { method: 'GET', url: `/api/app/order/${id}` },
      // skipHandleError: let the calling component decide how to handle 404
      { apiName: this.apiName, skipHandleError: true }
    );

  getItems = (id: number) =>
    this.restService.request<any, OrderItemDto[]>(
      { method: 'GET', url: `/api/app/order/${id}/order-items` },
      { apiName: this.apiName }
    );

  create = (input: CreateUpdateOrderDto) =>
    this.restService.request<any, OrderDto>(
      { method: 'POST', url: '/api/app/order/order-data', body: input },
      { apiName: this.apiName }
    );

  update = (id: number, input: CreateUpdateOrderDto) =>
    this.restService.request<any, OrderDto>(
      { method: 'PUT', url: `/api/app/order/${id}`, body: input },
      { apiName: this.apiName }
    );

  delete = (id: number) =>
    this.restService.request<any, void>(
      { method: 'DELETE', url: `/api/app/order/${id}` },
      { apiName: this.apiName }
    );

  updateStatus = (id: number, status: number) =>
    this.restService.request<any, OrderDto>(
      { method: 'PUT', url: `/api/app/order/${id}/update-status`, params: { status } },
      { apiName: this.apiName }
    );

  updateStage = (id: number, stage: number) =>
    this.restService.request<any, OrderDto>(
      { method: 'PUT', url: `/api/app/order/${id}/update-stage`, params: { stage } },
      { apiName: this.apiName }
    );
}

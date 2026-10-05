import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  CreateUpdateCustomerDto,
  CustomerDto,
  CustomerOverviewDto,
  CustomersPageDto,
  GetCustomersInput,
  ImportCustomersResultDto,
  LinkOrdersResultDto,
} from './models';

const BASE = '/api/app/customer';

@Injectable({ providedIn: 'root' })
export class CustomerService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  getOverview = () => this.req<CustomerOverviewDto>({ method: 'GET', url: `${BASE}/overview` });
  getList = (input: GetCustomersInput) => this.req<CustomersPageDto>({ method: 'GET', url: BASE, params: input });
  get = (id: number) => this.req<CustomerDto>({ method: 'GET', url: `${BASE}/${id}` });

  create = (input: CreateUpdateCustomerDto) => this.req<CustomerDto>({ method: 'POST', url: BASE, body: input });
  update = (id: number, input: CreateUpdateCustomerDto) => this.req<CustomerDto>({ method: 'PUT', url: `${BASE}/${id}`, body: input });
  delete = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}` });

  /** Attaches orders and invoices that carry this customer's phone but no customer yet. */
  linkMatching = (id: number) => this.req<LinkOrdersResultDto>({ method: 'POST', url: `${BASE}/${id}/link-matching` });

  /** Makes a customer out of each phone number in past orders that has none yet. */
  importFromOrders = () => this.req<ImportCustomersResultDto>({ method: 'POST', url: `${BASE}/import-from-orders` });
}

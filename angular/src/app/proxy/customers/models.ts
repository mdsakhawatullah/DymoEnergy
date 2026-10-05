export enum CustomerType { Household = 1, Business = 2, Dealer = 3, Institution = 4 }
export enum CustomerStatus { Active = 1, Inactive = 2, Blocked = 3 }
export enum CustomerSource { Storefront = 1, Showroom = 2, QuoteRequest = 3, Referral = 4, FieldSale = 5, PastOrders = 6, Other = 7 }

export const CUSTOMER_TYPES: { value: CustomerType; label: string; hint: string }[] = [
  { value: CustomerType.Household, label: 'Household', hint: 'A home buying for itself' },
  { value: CustomerType.Business, label: 'Business', hint: 'A shop, farm or factory' },
  { value: CustomerType.Dealer, label: 'Dealer', hint: 'Buys to resell, usually on terms' },
  { value: CustomerType.Institution, label: 'Institution', hint: 'School, mosque, NGO or office' },
];

export const CUSTOMER_STATUSES: { value: CustomerStatus; label: string }[] = [
  { value: CustomerStatus.Active, label: 'Active' },
  { value: CustomerStatus.Inactive, label: 'Inactive' },
  { value: CustomerStatus.Blocked, label: 'Blocked' },
];

export const CUSTOMER_SOURCES: { value: CustomerSource; label: string }[] = [
  { value: CustomerSource.Showroom, label: 'Came to the showroom' },
  { value: CustomerSource.Storefront, label: 'Ordered on the website' },
  { value: CustomerSource.QuoteRequest, label: 'Asked for a quote' },
  { value: CustomerSource.Referral, label: 'Referred by someone' },
  { value: CustomerSource.FieldSale, label: 'Met by the field team' },
  { value: CustomerSource.PastOrders, label: 'Built from past orders' },
  { value: CustomerSource.Other, label: 'Other' },
];

export interface GetCustomersInput {
  filter?: string;
  type?: CustomerType;
  status?: CustomerStatus;
  city?: string;
  owesMoney?: boolean;
  goneQuiet?: boolean;
  sorting?: string;
  skipCount: number;
  maxResultCount: number;
}

export interface CustomerListItemDto {
  id: number;
  name: string;
  phone?: string | null;
  email?: string | null;
  type: CustomerType;
  status: CustomerStatus;
  companyName?: string | null;
  where?: string | null;
  assignedTo?: string | null;
  tags: string[];
  orders: number;
  totalSpent: number;
  owed: number;
  lastOrderAt?: string | null;
  quietDays?: number | null;
}

export interface CustomerCountsDto { all: number; households: number; businesses: number; owesMoney: number; goneQuiet: number; }
export interface CustomersPageDto { totalCount: number; items: CustomerListItemDto[]; counts: CustomerCountsDto; }

export interface CustomerOverviewDto {
  total: number;
  newThisMonth: number;
  buyingThisMonth: number;
  owedTotal: number;
  owedCount: number;
  soldThisMonth: number;
  repeatCount: number;
  repeatPercent: number;
  quietCount: number;
  unlinkedOrders: number;
  wouldCreate: number;
  cities: string[];
}

export interface CustomerOrderDto { id: number; number: string; date: string; status: string; items?: string | null; total: number; due: number; loose: boolean; }
export interface CustomerInvoiceDto { id: number; number: string; date: string; dueDate?: string | null; total: number; paid: number; due: number; overdueDays?: number | null; }
export interface CustomerQuoteDto { id: number; date: string; status: string; interest?: string | null; location?: string | null; }

export interface CustomerDto {
  id: number;
  name: string;
  phone?: string | null;
  email?: string | null;
  type: CustomerType;
  status: CustomerStatus;
  source: CustomerSource;
  companyName?: string | null;
  taxId?: string | null;
  address?: string | null;
  area?: string | null;
  city?: string | null;
  district?: string | null;
  assignedTo?: string | null;
  note?: string | null;
  tags: string[];
  creditLimit: number;
  paymentTermDays: number;
  firstSeen?: string | null;
  createdAt: string;
  orderCount: number;
  totalSpent: number;
  owed: number;
  averageOrder: number;
  firstOrderAt?: string | null;
  lastOrderAt?: string | null;
  quietDays?: number | null;
  overCreditBy: number;
  orders: CustomerOrderDto[];
  invoices: CustomerInvoiceDto[];
  quotes: CustomerQuoteDto[];
  looseOrders: number;
}

export interface CreateUpdateCustomerDto {
  name: string;
  phone?: string | null;
  email?: string | null;
  type: CustomerType;
  status: CustomerStatus;
  source: CustomerSource;
  companyName?: string | null;
  taxId?: string | null;
  address?: string | null;
  area?: string | null;
  city?: string | null;
  district?: string | null;
  assignedTo?: string | null;
  note?: string | null;
  tags?: string | null;
  creditLimit: number;
  paymentTermDays: number;
  linkMatchingOrders: boolean;
}

export interface ImportCustomersResultDto { created: number; ordersLinked: number; invoicesLinked: number; skipped: number; message: string; }
export interface LinkOrdersResultDto { orders: number; invoices: number; }

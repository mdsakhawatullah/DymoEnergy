import { DymoPagedResultDto } from '../catalogues/models';

export { DymoPagedResultDto };

export interface CompanyDto {
  id: number;
  companyName: string;
  companyCode?: string;
  email?: string;
  phone?: string;
  website?: string;
  taxId?: string;
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  contactPersonName?: string;
  contactPersonEmail?: string;
  contactPersonPhone?: string;
  notes?: string;
  status: CompanyStatusType;

  sendStatementTo?: string;
  isParentCompany: boolean;
  hasChildCompany: boolean;
  parentCompanyId?: number;

  defaultShippingFee: number;
  freeShippingThreshold?: number;
  isFlatRateShipping: boolean;
  shippingCurrencyCode: string;

  externalAccountId?: string;
  apiKey?: string;
  webhookUrl?: string;
  isSyncEnabled: boolean;
  lastSyncedAt?: string;

  creationTime?: string;
  lastModificationTime?: string;
}

export interface CompanyFilterDto {
  filter?: string;
  status?: CompanyStatusType;
  isParentCompany?: boolean;
  parentCompanyId?: number;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface CreateUpdateCompanyDto {
  companyName: string;
  companyCode?: string;
  email?: string;
  phone?: string;
  website?: string;
  taxId?: string;
  address?: string;
  city?: string;
  state?: string;
  postalCode?: string;
  country?: string;
  contactPersonName?: string;
  contactPersonEmail?: string;
  contactPersonPhone?: string;
  notes?: string;
  status: CompanyStatusType;

  sendStatementTo?: string;
  isParentCompany: boolean;
  hasChildCompany: boolean;
  parentCompanyId?: number;

  defaultShippingFee: number;
  freeShippingThreshold?: number;
  isFlatRateShipping: boolean;
  shippingCurrencyCode: string;

  externalAccountId?: string;
  apiKey?: string;
  webhookUrl?: string;
  isSyncEnabled: boolean;
  lastSyncedAt?: string;
}

export type CompanyStatusType = 1 | 2;

export const CompanyStatusLabels: Record<number, string> = {
  1: 'Active',
  2: 'Inactive',
};

export const CompanyStatusColors: Record<number, string> = {
  1: 'success',
  2: 'default',
};

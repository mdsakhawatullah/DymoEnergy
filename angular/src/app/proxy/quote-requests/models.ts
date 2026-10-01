import { DymoPagedResultDto } from '../categories/models';

export { DymoPagedResultDto };

/** 1 New · 2 Contacted · 3 Closed · 4 Quoted (numbering is historical — see QuoteRequestStatusOrder). */
export type QuoteRequestStatusType = 1 | 2 | 3 | 4;

export interface QuoteRequestDto {
  id: number;
  name: string;
  phone?: string;
  email?: string;
  /** System type, e.g. "Residential rooftop". */
  interest?: string;
  message?: string;
  status: QuoteRequestStatusType;
  estimatedSize?: string;
  monthlyBill?: string;
  roofSite?: string;
  location?: string;
  adminNote?: string;
  creationTime: string;
  lastModificationTime?: string;
}

export interface QuoteRequestFilterDto {
  filter?: string;
  status?: QuoteRequestStatusType;
  interest?: string;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface QuoteRequestSummaryDto {
  allCount: number;
  newCount: number;
  contactedCount: number;
  quotedCount: number;
  closedCount: number;
  systemTypes: string[];
}

export interface UpdateQuoteRequestStatusDto {
  status: QuoteRequestStatusType;
}

export interface UpdateQuoteRequestDetailsDto {
  interest?: string;
  estimatedSize?: string;
  monthlyBill?: string;
  roofSite?: string;
  location?: string;
  adminNote?: string;
}

export const QuoteRequestStatusLabels: Record<number, string> = {
  1: 'New',
  2: 'Contacted',
  3: 'Closed',
  4: 'Quoted',
};

/** Pipeline order for tabs and the "Move to…" menu. */
export const QuoteRequestStatusOrder: QuoteRequestStatusType[] = [1, 2, 4, 3];

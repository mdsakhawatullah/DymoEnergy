import { DymoPagedResultDto } from '../catalogues/models';

export { DymoPagedResultDto };

export type QuoteRequestStatusType = 1 | 2 | 3;

export interface QuoteRequestDto {
  id: number;
  name: string;
  phone?: string;
  email?: string;
  interest?: string;
  message?: string;
  status: QuoteRequestStatusType;
  creationTime: string;
}

export interface QuoteRequestFilterDto {
  filter?: string;
  status?: QuoteRequestStatusType;
  sorting?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface UpdateQuoteRequestStatusDto {
  status: QuoteRequestStatusType;
}

export const QuoteRequestStatusLabels: Record<number, string> = {
  1: 'New',
  2: 'Contacted',
  3: 'Closed',
};

import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  GetLedgerLinesInput,
  LedgerAccessDto,
  LedgerCheckResultDto,
  LedgerHeaderDto,
  LedgerLineDetailDto,
  LedgerLinesPageDto,
  LedgerNeedsLookDto,
  LedgerProductDto,
  LedgerProductOptionDto,
  LedgerProofDto,
  LedgerSettingDto,
  ReviewLedgerLineDto,
} from './ledger.models';

const BASE = '/api/app/stock-ledger';

/** The stock ledger: every change to every quantity, and the proof that none of it was altered. */
@Injectable({ providedIn: 'root' })
export class StockLedgerService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  getHeader = () => this.req<LedgerHeaderDto>({ method: 'GET', url: `${BASE}/header` });

  // All movements
  getLines = (input: GetLedgerLinesInput) => this.req<LedgerLinesPageDto>({ method: 'GET', url: `${BASE}/lines`, params: input });
  getLine = (id: number) => this.req<LedgerLineDetailDto>({ method: 'GET', url: `${BASE}/${id}/line` });

  // One product
  getProductOptions = () => this.req<LedgerProductOptionDto[]>({ method: 'GET', url: `${BASE}/product-options` });
  getProduct = (productId: number, days?: number) =>
    this.req<LedgerProductDto>({ method: 'GET', url: `${BASE}/product/${productId}`, params: { days } });

  // Needs a look
  getNeedsLook = () => this.req<LedgerNeedsLookDto>({ method: 'GET', url: `${BASE}/needs-look` });
  reviewLine = (id: number, input: ReviewLedgerLineDto) =>
    this.req<LedgerLineDetailDto>({ method: 'POST', url: `${BASE}/${id}/review-line`, body: input });

  // Who has access
  getAccess = () => this.req<LedgerAccessDto>({ method: 'GET', url: `${BASE}/access` });

  // Proof & keeping
  getProof = () => this.req<LedgerProofDto>({ method: 'GET', url: `${BASE}/proof` });
  verifyChain = () => this.req<LedgerCheckResultDto>({ method: 'POST', url: `${BASE}/verify-chain` });

  updateSetting = (input: LedgerSettingDto) => this.req<LedgerSettingDto>({ method: 'PUT', url: `${BASE}/setting`, body: input });
}

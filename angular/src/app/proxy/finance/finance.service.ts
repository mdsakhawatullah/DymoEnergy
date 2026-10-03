import { RestService } from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
  CreateFinanceTransactionDto,
  CreateFinanceTransferDto,
  CreateUpdateFinanceAccountDto,
  CreateUpdateFinanceBillDto,
  CreateUpdateFinanceCategoryDto,
  CreateUpdateFinanceExpenseDto,
  CreateUpdateFinanceItemDto,
  CreateUpdateFinanceRecurringDto,
  FinanceAccountDto,
  FinanceBillDto,
  FinanceBillsDto,
  FinanceCashDto,
  FinanceCategoryDto,
  FinanceDuesDto,
  FinanceExpenseDto,
  FinanceExpensesDto,
  FinanceItemDto,
  FinanceOverviewDto,
  FinancePnlDto,
  FinanceRecurringDto,
  FinanceSettingDto,
  PayFinanceBillDto,
  PayRecurringDto,
  ReceiveInTransitDto,
  UpdateFinanceSettingDto,
} from './models';

const BASE = '/api/app/finance';

@Injectable({ providedIn: 'root' })
export class FinanceService {
  apiName = 'Default';
  private readonly filesUrl = `${environment.apis['default'].url}/api/app/finance-files`;

  constructor(
    private restService: RestService,
    private http: HttpClient,
  ) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any }) {
    return this.restService.request<any, T>(request, { apiName: this.apiName });
  }

  // ── Page views ──────────────────────────────────────────────────────────
  getOverview = (period: string) => this.req<FinanceOverviewDto>({ method: 'GET', url: `${BASE}/overview`, params: { period } });
  getCash = (period: string) => this.req<FinanceCashDto>({ method: 'GET', url: `${BASE}/cash`, params: { period } });
  getDues = (period: string) => this.req<FinanceDuesDto>({ method: 'GET', url: `${BASE}/dues`, params: { period } });
  getBills = (period: string) => this.req<FinanceBillsDto>({ method: 'GET', url: `${BASE}/bills`, params: { period } });
  getExpenses = (period: string) => this.req<FinanceExpensesDto>({ method: 'GET', url: `${BASE}/expenses`, params: { period } });
  getPnl = (period: string) => this.req<FinancePnlDto>({ method: 'GET', url: `${BASE}/pnl`, params: { period } });

  updateSetting = (input: UpdateFinanceSettingDto) => this.req<FinanceSettingDto>({ method: 'PUT', url: `${BASE}/setting`, body: input });

  // ── Accounts ────────────────────────────────────────────────────────────
  createAccount = (input: CreateUpdateFinanceAccountDto) => this.req<FinanceAccountDto>({ method: 'POST', url: `${BASE}/account`, body: input });
  updateAccount = (id: number, input: CreateUpdateFinanceAccountDto) => this.req<FinanceAccountDto>({ method: 'PUT', url: `${BASE}/${id}/account`, body: input });
  deleteAccount = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/account` });
  markAccountMatched = (id: number) => this.req<FinanceAccountDto>({ method: 'PUT', url: `${BASE}/${id}/account-reconciled` });

  // ── Categories ──────────────────────────────────────────────────────────
  createCategory = (input: CreateUpdateFinanceCategoryDto) => this.req<FinanceCategoryDto>({ method: 'POST', url: `${BASE}/category`, body: input });
  updateCategory = (id: number, input: CreateUpdateFinanceCategoryDto) => this.req<FinanceCategoryDto>({ method: 'PUT', url: `${BASE}/${id}/category`, body: input });
  deleteCategory = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/category` });

  // ── Monthly costs ───────────────────────────────────────────────────────
  createRecurring = (input: CreateUpdateFinanceRecurringDto) => this.req<FinanceRecurringDto>({ method: 'POST', url: `${BASE}/recurring`, body: input });
  updateRecurring = (id: number, input: CreateUpdateFinanceRecurringDto) => this.req<FinanceRecurringDto>({ method: 'PUT', url: `${BASE}/${id}/recurring`, body: input });
  deleteRecurring = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/recurring` });
  payRecurring = (id: number, input: PayRecurringDto) => this.req<FinanceExpenseDto>({ method: 'POST', url: `${BASE}/${id}/recurring-payment`, body: input });

  // ── Generic lists ───────────────────────────────────────────────────────
  createItem = (input: CreateUpdateFinanceItemDto) => this.req<FinanceItemDto>({ method: 'POST', url: `${BASE}/item`, body: input });
  updateItem = (id: number, input: CreateUpdateFinanceItemDto) => this.req<FinanceItemDto>({ method: 'PUT', url: `${BASE}/${id}/item`, body: input });
  deleteItem = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/item` });
  receiveItem = (id: number, input: ReceiveInTransitDto) => this.req<void>({ method: 'POST', url: `${BASE}/${id}/item-receipt`, body: input });

  // ── Expenses & bills ────────────────────────────────────────────────────
  createExpense = (input: CreateUpdateFinanceExpenseDto) => this.req<FinanceExpenseDto>({ method: 'POST', url: `${BASE}/expense`, body: input });
  updateExpense = (id: number, input: CreateUpdateFinanceExpenseDto) => this.req<FinanceExpenseDto>({ method: 'PUT', url: `${BASE}/${id}/expense`, body: input });
  deleteExpense = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/expense` });

  createBill = (input: CreateUpdateFinanceBillDto) => this.req<FinanceBillDto>({ method: 'POST', url: `${BASE}/bill`, body: input });
  updateBill = (id: number, input: CreateUpdateFinanceBillDto) => this.req<FinanceBillDto>({ method: 'PUT', url: `${BASE}/${id}/bill`, body: input });
  deleteBill = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/bill` });
  payBill = (id: number, input: PayFinanceBillDto) => this.req<FinanceBillDto>({ method: 'POST', url: `${BASE}/${id}/bill-payment`, body: input });
  deleteBillPayment = (id: number, paymentId: number) =>
    this.req<FinanceBillDto>({ method: 'DELETE', url: `${BASE}/${id}/bill-payment/${paymentId}` });

  // ── Ledger ──────────────────────────────────────────────────────────────
  createTransaction = (input: CreateFinanceTransactionDto) => this.req<void>({ method: 'POST', url: `${BASE}/transaction`, body: input });
  createTransfer = (input: CreateFinanceTransferDto) => this.req<void>({ method: 'POST', url: `${BASE}/transfer`, body: input });
  deleteTransaction = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/transaction` });

  // ── Files ───────────────────────────────────────────────────────────────
  uploadReceipt(file: File): Observable<{ url: string; fileName: string; sizeBytes: number }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string; fileName: string; sizeBytes: number }>(`${this.filesUrl}/upload`, form);
  }

  exportForAccountant(period: string): Observable<Blob> {
    return this.http.get(`${this.filesUrl}/export`, { params: { period }, responseType: 'blob' });
  }
}

export enum FinanceAccountKind { Cash = 1, MobileWallet = 2, Bank = 3 }
export enum FinanceDirection { In = 1, Out = 2 }
export enum FinanceCostGroup { CostOfSales = 1, Running = 2 }
export enum FinanceItemKind { InTransit = 1, Advance = 2, LetterOfCredit = 3, DealerCredit = 4, Insight = 5 }

export interface FinanceLabelDto { key: string; group: string; caption: string; value: string; }

export interface FinanceSettingDto {
  accentColor: string;
  currencySymbol: string;
  compactMoney: boolean;
  dueSoonDays: number;
  agingStep1: number;
  agingStep2: number;
  chartMonths: number;
  reminderTemplate?: string;
  labels: FinanceLabelDto[];
}

export interface UpdateFinanceSettingDto {
  accentColor: string;
  currencySymbol: string;
  compactMoney: boolean;
  dueSoonDays: number;
  agingStep1: number;
  agingStep2: number;
  chartMonths: number;
  reminderTemplate?: string | null;
  labels: Record<string, string>;
}

export interface FinancePeriodDto { key: string; label: string; prevLabel: string; from: string; to: string; }
export interface FinancePeriodOptionDto { key: string; label: string; }

export interface FinanceAccountDto {
  id: number;
  name: string;
  kind: FinanceAccountKind;
  shortCode: string;
  color: string;
  openingBalance: number;
  openingDate: string;
  paymentMethods: string[];
  lastReconciledOn?: string | null;
  isActive: boolean;
  order: number;
  balance: number;
  statusText: string;
  statusTone: 'green' | 'amber';
}

export interface CreateUpdateFinanceAccountDto {
  name: string;
  kind: FinanceAccountKind;
  shortCode: string;
  color: string;
  openingBalance: number;
  openingDate: string;
  paymentMethods: string[];
  isActive: boolean;
  order: number;
}

export interface FinanceCategoryDto {
  id: number; name: string; color: string; costGroup: FinanceCostGroup; plLine: string; order: number; isActive: boolean;
}
export interface CreateUpdateFinanceCategoryDto {
  name: string; color: string; costGroup: FinanceCostGroup; plLine?: string | null; order: number; isActive: boolean;
}

export interface FinanceRecurringDto {
  id: number; name: string; detail?: string; dayOfMonth: number; amount: number; categoryId: number;
  accountId?: number | null; isActive: boolean; order: number; status: 'paid' | 'due' | 'scheduled';
}
export interface CreateUpdateFinanceRecurringDto {
  name: string; detail?: string | null; dayOfMonth: number; amount: number; categoryId: number;
  accountId?: number | null; isActive: boolean; order: number;
}
export interface PayRecurringDto { date?: string | null; accountId?: number | null; amount?: number | null; }

export interface FinanceItemDto {
  id: number; kind: FinanceItemKind; title: string; detail?: string; extra?: string; color?: string;
  amount: number; date?: string | null; percent?: number | null; flag: boolean; order: number;
}
export interface CreateUpdateFinanceItemDto {
  kind: FinanceItemKind; title: string; detail?: string | null; extra?: string | null; color?: string | null;
  amount: number; date?: string | null; percent?: number | null; flag: boolean; order: number;
}
export interface ReceiveInTransitDto { accountId: number; date?: string | null; }

export interface FinanceExpenseDto {
  id: number; date: string; categoryId: number; categoryName: string; categoryColor: string; description: string;
  amount: number; accountId?: number | null; paidBy: string; paidByNote?: string; receiptUrl?: string; recurringCostId?: number | null;
}
export interface CreateUpdateFinanceExpenseDto {
  date: string; categoryId: number; description: string; amount: number;
  accountId?: number | null; paidByNote?: string | null; receiptUrl?: string | null;
}

export interface FinanceBillPaymentDto {
  id: number; date: string; amount: number; accountId: number; accountName: string; reference?: string;
}
export interface FinanceBillDto {
  id: number; supplier: string; description?: string; billNumber?: string; billDate: string; dueDate: string; amount: number;
  categoryId: number; categoryName: string; receiptUrl?: string; note?: string;
  paid: number; remaining: number; status: 'paid' | 'part' | 'open' | 'overdue'; dueInDays: number; lastPaidOn?: string | null;
  payments: FinanceBillPaymentDto[];
}
export interface CreateUpdateFinanceBillDto {
  supplier: string; description?: string | null; billNumber?: string | null; billDate: string; dueDate: string;
  amount: number; categoryId: number; receiptUrl?: string | null; note?: string | null;
}
export interface PayFinanceBillDto { amount: number; accountId: number; date?: string | null; reference?: string | null; }

export interface CreateFinanceTransactionDto {
  direction: FinanceDirection; accountId: number; amount: number; date?: string | null;
  category?: string | null; description?: string | null; reference?: string | null;
}
export interface CreateFinanceTransferDto { fromAccountId: number; toAccountId: number; amount: number; date?: string | null; note?: string | null; }

export interface FinanceMovementDto {
  source: 'tx' | 'invoice'; refId: number; date: string; direction: FinanceDirection; title: string; category?: string;
  accountId?: number | null; accountName: string; amount: number; balanceAfter?: number | null; isTransfer: boolean;
}

export interface FinanceUnassignedDto { count: number; amount: number; methods: string[]; }

export interface FinanceOverviewDto {
  setting: FinanceSettingDto;
  period: FinancePeriodDto;
  periods: FinancePeriodOptionDto[];
  moneyToUse: number;
  accountCount: number;
  onTheWayTotal: number;
  onTheWayCount: number;
  onTheWayLatestDays?: number | null;
  customersOwe: number;
  customersOweCount: number;
  lateInvoices: number;
  suppliersOwe: number;
  openBills: number;
  overdueBills: number;
  profit: number;
  profitPeriodLabel: string;
  unassigned: FinanceUnassignedDto;
  accounts: FinanceAccountDto[];
  categories: FinanceCategoryDto[];
  allAccounts: FinanceAccountDto[];
  allCategories: FinanceCategoryDto[];
  recurring: FinanceRecurringDto[];
  items: FinanceItemDto[];
}

export interface FinanceFlowLineDto { label: string; amount: number; }
export interface FinanceCashDto {
  accounts: FinanceAccountDto[];
  onTheWay: FinanceItemDto[];
  moneyIn: number;
  moneyOut: number;
  inLines: FinanceFlowLineDto[];
  outLines: FinanceFlowLineDto[];
  movements: FinanceMovementDto[];
}

export interface FinanceDueDto {
  invoiceId: number; invoiceNumber: string; customerName: string; customerPhone?: string; channel?: string;
  invoiceDate: string; dueDate?: string | null; balance: number; daysLate?: number | null;
}
export interface FinanceAgingDto { key: string; label: string; amount: number; count: number; }
export interface FinanceDealerDto {
  id: number; name: string; detail?: string; limit: number; used: number; left: number; openInvoices: FinanceDueDto[];
}
export interface FinanceDuesDto {
  total: number; count: number; lateAmount: number; lateCount: number; collected: number;
  collectedPercentOfSales?: number | null; averageDaysToPay?: number | null; previousAverageDaysToPay?: number | null;
  aging: FinanceAgingDto[]; advances: FinanceItemDto[]; advancesTotal: number; dues: FinanceDueDto[]; dealers: FinanceDealerDto[];
}

export interface FinancePlanWeekDto { label: string; toPay: number; expected: number; }
export interface FinanceBillsDto {
  owe: number; openCount: number; dueSoonAmount: number; dueSoonCount: number; overdueAmount: number; overdueCount: number;
  overdueMaxDays: number; paidInPeriod: number; paidCount: number;
  bills: FinanceBillDto[]; plan: FinancePlanWeekDto[]; tightWeek?: string | null; lettersOfCredit: FinanceItemDto[];
}

export interface FinanceCostLineDto {
  categoryId: number; name: string; color: string; amount: number; count: number; changePercent?: number | null;
}
export interface FinanceExpensesDto {
  running: number; runningPercentOfSales?: number | null; biggestName?: string; biggestAmount: number; biggestCount: number;
  upMostName?: string; upMostPercent?: number | null; costPerOrder?: number | null; orderCount: number; totalSpent: number;
  categories: FinanceCostLineDto[]; recurring: FinanceRecurringDto[]; recurringTotal: number;
  latest: FinanceExpenseDto[]; periodExpenseCount: number;
}

export interface FinancePnlLineDto {
  section: 'sales' | 'cost' | 'running'; label: string; current: number; previous: number; changePercent?: number | null;
  isTotal: boolean; key: string;
}
export interface FinanceBarDto { label: string; profit: number; isCurrent: boolean; }
export interface FinanceInsightDto { kind: 'best' | 'dip'; label: string; amount: number; percentVsAverage?: number | null; }
export interface FinancePnlDto { lines: FinancePnlLineDto[]; bars: FinanceBarDto[]; insights: FinanceInsightDto[]; notes: FinanceItemDto[]; }

// Mirrors DymoEnergy.Analytics DTOs (AnalyticsDtos.cs).

export interface AnalyticsFilterDto {
  dateFrom?: string;
  dateTo?: string;
  channel?: string;
}

export interface NamedValueDto {
  name: string;
  value: number;
  /** 0–1 share of the list total. */
  share: number;
  /** % change vs the previous period; null when there is no baseline. */
  change?: number | null;
  count?: number | null;
}

export interface AnalyticsAlertDto {
  tone: 'red' | 'amber' | 'blue' | 'green';
  title: string;
  text: string;
  action?: string;
  link?: string;
  count?: number;
  reportKey?: string;
}

// ── Overview ────────────────────────────────────────────────────────────────

export interface AnalyticsWeekDto {
  label: string;
  /** First / last Bangladesh-local day of the week chunk. */
  from: string;
  to: string;
  sales: number;
  orders: number;
  change?: number | null;
  isBest: boolean;
  /** Every day in the chunk, quiet days included. */
  days: AnalyticsDailyDto[];
}

export interface AnalyticsLatestSaleDto {
  channel: string;
  summary: string;
  total: number;
  date: string;
  invoiceId?: number;
}

export interface AnalyticsOverviewDto {
  sales: number;
  previousSales: number;
  salesChange?: number | null;
  monthlyTarget: number;
  weeks: AnalyticsWeekDto[];
  todaySales: number;
  todayOrders: number;
  latestSales: AnalyticsLatestSaleDto[];
  orders: number;
  ordersChange?: number | null;
  averageOrder: number;
  quoteRequests: number;
  quotesWon: number;
  quoteWinRate: number;
  /** Percentage-point change of the win rate. */
  quoteWinChange?: number | null;
  moneyStillDue: number;
  overdueInvoices: number;
  overdueAmount: number;
  solarKw: number;
  solarKwChange?: number | null;
  panelsSold: number;
  alerts: AnalyticsAlertDto[];
  topProducts: NamedValueDto[];
  channels: NamedValueDto[];
}

// ── Sales ───────────────────────────────────────────────────────────────────

export interface AnalyticsDailyDto { date: string; sales: number; orders: number; }
export interface AnalyticsHeatmapDto { days: string[]; hours: number[]; cells: number[][]; peakWindow?: string; }
export interface AnalyticsFunnelStepDto { name: string; count: number; rate?: number | null; }
export interface AnalyticsSalespersonDto { name: string; orders: number; sales: number; average: number; }

export interface AnalyticsSalesDto {
  sales: number;
  salesChange?: number | null;
  orders: number;
  ordersChange?: number | null;
  ordersPerDay: number;
  averageOrder: number;
  averageChange?: number | null;
  quoteRequests: number;
  quotesWon: number;
  quoteWinRate: number;
  quoteWinChange?: number | null;
  daily: AnalyticsDailyDto[];
  byChannel: NamedValueDto[];
  byCategory: NamedValueDto[];
  heatmap: AnalyticsHeatmapDto;
  funnel: AnalyticsFunnelStepDto[];
  salespeople: AnalyticsSalespersonDto[];
  discounts: NamedValueDto[];
  discountTotal: number;
}

// ── Products & stock ────────────────────────────────────────────────────────

export interface AnalyticsProductRowDto {
  productId?: number;
  name: string;
  category: string;
  sold: number;
  sales: number;
  share: number;
  stockLeft?: number | null;
  daysLeft?: number | null;
  stockState: 'ok' | 'low' | 'out' | 'none';
}

export interface AnalyticsReorderDto {
  productId: number;
  name: string;
  stock: number;
  soldLast30: number;
  daysLeft?: number | null;
  suggestedQty: number;
}

export interface AnalyticsSlowStockDto { productId: number; name: string; stock: number; value: number; lastSold?: string | null; }

export interface AnalyticsProductsDto {
  stockValue: number;
  productsInStock: number;
  unitsInStock: number;
  daysOfStockLeft?: number | null;
  outOfStock: number;
  runningLow: number;
  bestSellers: AnalyticsProductRowDto[];
  reorder: AnalyticsReorderDto[];
  slowStock: AnalyticsSlowStockDto[];
  stockByCategory: NamedValueDto[];
}

// ── Money ───────────────────────────────────────────────────────────────────

export interface AnalyticsAgingDto { name: string; amount: number; invoices: number; tone: 'green' | 'amber' | 'red'; }
export interface AnalyticsDebtorDto { name: string; phone?: string; invoices: number; due: number; lateDays?: number | null; note?: string; }
export interface AnalyticsReminderDto { invoiceId: number; invoiceNumber: string; name: string; phone?: string; due: number; lateDays: number; }

export interface AnalyticsMoneyDto {
  collected: number;
  collectedChange?: number | null;
  stillDue: number;
  dueInvoices: number;
  dueCustomers: number;
  overdue: number;
  overdueInvoices: number;
  refunds: number;
  refundCount: number;
  refundsChange?: number | null;
  byMethod: NamedValueDto[];
  digitalShare: number;
  aging: AnalyticsAgingDto[];
  debtors: AnalyticsDebtorDto[];
  expected: NamedValueDto[];
  lateInvoices: AnalyticsReminderDto[];
  vatInSales: number;
  vatOnRefunds: number;
}

// ── Customers ───────────────────────────────────────────────────────────────

export interface AnalyticsTopCustomerDto { name: string; location?: string; phone?: string; orders: number; spent: number; }

export interface AnalyticsCustomersDto {
  buyers: number;
  buyersChange?: number | null;
  newCustomers: number;
  newChange?: number | null;
  returning: number;
  returningRate: number;
  warrantyEndingSoon: number;
  locations: NamedValueDto[];
  topCustomers: AnalyticsTopCustomerDto[];
  followUps: AnalyticsAlertDto[];
}

// ── Reports ─────────────────────────────────────────────────────────────────

export type ReportColumnType = 'text' | 'money' | 'number' | 'percent' | 'date' | 'dateyear' | 'month' | 'chip' | 'share';
export type ReportTone = 'green' | 'amber' | 'red' | 'blue' | 'purple' | 'muted';
export type ReportKpiTone = 'neutral' | 'green' | 'amber' | 'red' | 'dark';

export interface AnalyticsReportColumnDto {
  label: string;
  type: ReportColumnType;
  bold?: boolean;
  muted?: boolean;
  mono?: boolean;
}

export interface AnalyticsReportKpiDto {
  label: string;
  value: string | number | null;
  type: 'money' | 'number' | 'percent' | 'text' | 'date';
  sub?: string | null;
  /** Shown as "▲ 14% vs August" (sub supplies "vs August"). */
  change?: number | null;
  tone: ReportKpiTone;
}

/** A report shaped after the "DymoEnergy report templates" PDF design. */
export interface AnalyticsReportDto {
  key: string;
  title: string;
  subtitle: string;
  /** "Sales" · "Products & stock" · "Money" · "Customers" */
  category: string;
  reportId: string;
  kpis: AnalyticsReportKpiDto[];
  steps?: AnalyticsReportKpiDto[] | null;
  barsTitle?: string | null;
  bars?: NamedValueDto[] | null;
  barsFormat: 'money' | 'number';
  tableTitle?: string | null;
  columns: AnalyticsReportColumnDto[];
  rows: (string | number | null)[][];
  /** Per-cell tone, same shape as rows. */
  tones?: (ReportTone | null)[][] | null;
  totals?: (string | number | null)[] | null;
  totalsLabel: string;
  facts?: { title: string; text: string }[] | null;
  note?: string | null;
  noteTone: 'neutral' | 'amber';
}

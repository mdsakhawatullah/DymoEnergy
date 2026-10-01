import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { SharedModule } from '../../shared/shared.module';
import { SalesInvoiceService } from '../../proxy/sales-invoices/sales-invoice.service';
import {
  SalesInvoiceDto,
  SalesInvoiceFilterDto,
  SalesInvoiceItemDto,
  SalesInvoicePaymentStateType,
  SalesInvoiceShortStatus,
  SalesInvoiceStatusLabels,
  SalesInvoiceSummaryDto,
} from '../../proxy/sales-invoices/models';
import { SalesInvoiceEntryDrawerComponent } from './entry-drawer/sales-invoice-entry-drawer.component';
import { InvoiceQuickViewComponent } from './quick-view/invoice-quick-view.component';
import { TakaPipe } from './invoice-format';

type TabKey    = 'all' | 'paid' | 'due' | 'overdue';
/** 'custom' = a from/to range passed in the URL (e.g. a week clicked on Analytics). */
type PeriodKey = 'this-month' | 'last-month' | 'last-90' | 'this-year' | 'all' | 'custom';

const TAB_STATE: Record<TabKey, SalesInvoicePaymentStateType | undefined> = {
  all: undefined, paid: 1, due: 2, overdue: 3,
};

@Component({
  selector:    'app-sales-invoices',
  templateUrl: './sales-invoices.component.html',
  styleUrl:    './sales-invoices.component.css',
  imports:     [SharedModule, SalesInvoiceEntryDrawerComponent, InvoiceQuickViewComponent, TakaPipe],
})
export class SalesInvoicesComponent implements OnInit, OnDestroy {

  // ── List ──────────────────────────────────────────────────────────────────
  invoices:  SalesInvoiceDto[] = [];
  totalCount = 0;
  pageIndex  = 1;
  readonly pageSize = 10;
  filter     = '';
  loading    = false;
  exporting  = false;
  activeTab: TabKey    = 'all';
  period:    PeriodKey = 'this-month';

  summary: SalesInvoiceSummaryDto = {
    salesTotal: 0, collected: 0, stillDue: 0,
    allCount: 0, paidCount: 0, dueCount: 0, overdueCount: 0,
  };

  readonly tabs: { key: TabKey; label: string; count: (s: SalesInvoiceSummaryDto) => number }[] = [
    { key: 'all',     label: 'All',     count: s => s.allCount     },
    { key: 'paid',    label: 'Paid',    count: s => s.paidCount    },
    { key: 'due',     label: 'Due',     count: s => s.dueCount     },
    { key: 'overdue', label: 'Overdue', count: s => s.overdueCount },
  ];

  /** Local calendar days of the 'custom' period, inclusive. */
  private customRange: { from: Date; to: Date } | null = null;

  periods: { key: PeriodKey; label: string; statLabel: string }[] = [
    { key: 'this-month', label: 'This month',   statLabel: 'this month'   },
    { key: 'last-month', label: 'Last month',   statLabel: 'last month'   },
    { key: 'last-90',    label: 'Last 90 days', statLabel: 'last 90 days' },
    { key: 'this-year',  label: 'This year',    statLabel: 'this year'    },
    { key: 'all',        label: 'All time',     statLabel: 'all time'     },
  ];

  readonly shortStatus = SalesInvoiceShortStatus;

  // ── Quick view ────────────────────────────────────────────────────────────
  quickViewId: number | null = null;

  // ── Create / edit drawer ──────────────────────────────────────────────────
  isEntryOpen   = false;
  editInvoice:  SalesInvoiceDto | null = null;
  editItems:    SalesInvoiceItemDto[]  = [];

  private search$ = new Subject<string>();
  private subs    = new Subscription();

  constructor(
    private invoiceService: SalesInvoiceService,
    private message: NzMessageService,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.applyUrlRange();
    this.subs.add(
      this.search$.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {
        this.pageIndex = 1;
        this.reload();
      }),
    );
    this.reload();
  }

  ngOnDestroy(): void { this.subs.unsubscribe(); }

  /** ?from=2026-09-08&to=2026-09-14 → a "8–14 Sep" period option, selected. */
  private applyUrlRange(): void {
    const q    = this.route.snapshot.queryParamMap;
    const from = this.parseDay(q.get('from'));
    const to   = this.parseDay(q.get('to')) ?? from;
    if (!from || !to || to < from) return;

    this.customRange = { from, to };
    const fmt   = (d: Date, month = true) => d.toLocaleDateString('en-GB', month ? { day: 'numeric', month: 'short' } : { day: 'numeric' });
    const label = from.getMonth() === to.getMonth() ? `${fmt(from, false)}–${fmt(to)}` : `${fmt(from)} – ${fmt(to)}`;
    this.periods = [...this.periods, { key: 'custom', label, statLabel: label }];
    this.period  = 'custom';
  }

  /** "2026-09-08" as a local date (not UTC midnight). */
  private parseDay(v: string | null): Date | null {
    const m = v?.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    return m ? new Date(+m[1], +m[2] - 1, +m[3]) : null;
  }

  // ── Derived ───────────────────────────────────────────────────────────────

  get periodStatLabel(): string {
    return this.periods.find(p => p.key === this.period)?.statLabel ?? '';
  }

  get pageCount(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }

  get quickViewPosition(): number {
    const idx = this.invoices.findIndex(i => i.id === this.quickViewId);
    return idx < 0 ? 0 : (this.pageIndex - 1) * this.pageSize + idx + 1;
  }

  paidPct(inv: SalesInvoiceDto): number {
    if (inv.grandTotal <= 0) return inv.status === 3 ? 100 : 0;
    return Math.min(100, Math.round((inv.amountPaid / inv.grandTotal) * 100));
  }

  // ── Data ──────────────────────────────────────────────────────────────────

  reload(): void {
    this.loadSummary();
    this.loadData();
  }

  private dateRange(): { dateFrom?: string; dateTo?: string } {
    const now = new Date();
    const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate());
    switch (this.period) {
      case 'this-month':
        return { dateFrom: new Date(now.getFullYear(), now.getMonth(), 1).toISOString() };
      case 'last-month':
        return {
          dateFrom: new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString(),
          dateTo:   new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0, -1).toISOString(),
        };
      case 'last-90': {
        const from = startOfDay(now); from.setDate(from.getDate() - 89);
        return { dateFrom: from.toISOString() };
      }
      case 'this-year':
        return { dateFrom: new Date(now.getFullYear(), 0, 1).toISOString() };
      case 'custom': {
        if (!this.customRange) return {};
        const { from, to } = this.customRange;
        return {
          dateFrom: from.toISOString(),
          dateTo:   new Date(to.getFullYear(), to.getMonth(), to.getDate() + 1, 0, 0, 0, -1).toISOString(),
        };
      }
      default:
        return {};
    }
  }

  private buildFilter(): SalesInvoiceFilterDto {
    return {
      filter:       this.filter.trim() || undefined,
      paymentState: TAB_STATE[this.activeTab],
      ...this.dateRange(),
    };
  }

  loadSummary(): void {
    this.invoiceService.getSummary({ filter: this.filter.trim() || undefined, ...this.dateRange() })
      .subscribe(s => this.summary = s);
  }

  loadData(after?: () => void): void {
    this.loading = true;
    this.invoiceService.getListData({
      ...this.buildFilter(),
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    }).subscribe({
      next: result => {
        this.invoices   = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
        after?.();
      },
      error: () => { this.loading = false; },
    });
  }

  // ── Filters & paging ──────────────────────────────────────────────────────

  selectTab(tab: TabKey): void   { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearchInput(v: string): void { this.search$.next(v); }
  onPeriodChange(): void         { this.pageIndex = 1; this.reload(); }

  prevPage(after?: () => void): void {
    if (this.pageIndex <= 1) return;
    this.pageIndex--;
    this.loadData(after);
  }

  nextPage(after?: () => void): void {
    if (this.pageIndex >= this.pageCount) return;
    this.pageIndex++;
    this.loadData(after);
  }

  // ── Quick view ────────────────────────────────────────────────────────────

  openQuickView(inv: SalesInvoiceDto): void { this.quickViewId = inv.id; }
  closeQuickView(): void                   { this.quickViewId = null; }

  quickViewPrev(): void {
    const idx = this.invoices.findIndex(i => i.id === this.quickViewId);
    if (idx > 0) this.quickViewId = this.invoices[idx - 1].id;
    else if (idx === 0) this.prevPage(() => this.quickViewId = this.invoices[this.invoices.length - 1]?.id ?? null);
  }

  quickViewNext(): void {
    const idx = this.invoices.findIndex(i => i.id === this.quickViewId);
    if (idx >= 0 && idx < this.invoices.length - 1) this.quickViewId = this.invoices[idx + 1].id;
    else if (idx === this.invoices.length - 1) this.nextPage(() => this.quickViewId = this.invoices[0]?.id ?? null);
  }

  /** Patch the row in place so the list reflects a payment without losing scroll/page. */
  onQuickViewChanged(updated: SalesInvoiceDto | null): void {
    if (updated) {
      const idx = this.invoices.findIndex(i => i.id === updated.id);
      if (idx >= 0) this.invoices = this.invoices.map((i, n) => n === idx ? { ...i, ...updated } : i);
      this.loadSummary();
    } else {
      this.reload();
    }
  }

  // ── Create / edit ─────────────────────────────────────────────────────────

  openCreate(): void {
    this.editInvoice = null;
    this.editItems   = [];
    this.isEntryOpen = true;
  }

  openEdit(e: { invoice: SalesInvoiceDto; items: SalesInvoiceItemDto[] }): void {
    this.editInvoice = e.invoice;
    this.editItems   = e.items;
    this.isEntryOpen = true;
  }

  onEntryClosed(): void { this.isEntryOpen = false; }

  onInvoiceSaved(saved?: SalesInvoiceDto): void {
    this.isEntryOpen = false;
    this.reload();
    // Re-open (or refresh) the quick view on the saved invoice
    if (saved?.id) {
      this.quickViewId = null;
      setTimeout(() => this.quickViewId = saved.id);
    }
  }

  // ── Export ────────────────────────────────────────────────────────────────

  exportCsv(): void {
    this.exporting = true;
    this.invoiceService.getListData({ ...this.buildFilter(), skipCount: 0, maxResultCount: 1000 }).subscribe({
      next: result => {
        const esc  = (v: unknown) => `"${String(v ?? '').replace(/"/g, '""')}"`;
        const head = ['Invoice', 'Date', 'Due date', 'Customer', 'Phone', 'Items', 'Channel', 'Total', 'Paid', 'Due', 'Status'];
        const rows = result.items.map(i => [
          i.invoiceNumber,
          i.invoiceDate?.slice(0, 10),
          i.dueDate?.slice(0, 10) ?? '',
          i.customerName,
          i.customerPhone,
          i.firstItemName ? `${i.firstItemName}${i.itemCount > 1 ? ` + ${i.itemCount - 1} more` : ''}` : '',
          i.channel,
          i.grandTotal,
          i.amountPaid,
          i.balanceDue,
          SalesInvoiceStatusLabels[i.status],
        ].map(esc).join(','));

        // BOM so Excel opens the ৳ / Bangla text as UTF-8
        const blob = new Blob(['﻿' + [head.join(','), ...rows].join('\r\n')], { type: 'text/csv;charset=utf-8' });
        const a    = document.createElement('a');
        a.href     = URL.createObjectURL(blob);
        a.download = `sale-invoices-${this.period}-${new Date().toISOString().slice(0, 10)}.csv`;
        a.click();
        URL.revokeObjectURL(a.href);

        this.exporting = false;
        if (result.totalCount > result.items.length) {
          this.message.info(`Exported the latest ${result.items.length} of ${result.totalCount} invoices.`);
        }
      },
      error: () => { this.exporting = false; },
    });
  }
}

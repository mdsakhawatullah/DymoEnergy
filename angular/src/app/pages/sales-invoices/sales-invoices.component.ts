import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { SalesInvoiceService } from '../../proxy/sales-invoices/sales-invoice.service';
import {
  SalesInvoiceDto,
  SalesInvoiceItemDto,
  SalesInvoiceStatusColors,
  SalesInvoiceStatusLabels,
} from '../../proxy/sales-invoices/models';
import { SalesInvoiceEntryDrawerComponent } from './entry-drawer/sales-invoice-entry-drawer.component';

type TabKey   = 'all' | 'draft' | 'issued' | 'paid' | 'overdue';
type StatsKey = 'total' | 'draft' | 'issued' | 'paid' | 'overdue';

interface InvoiceStats {
  total:   number;
  draft:   number;
  issued:  number;
  paid:    number;
  overdue: number;
}

@Component({
  selector:    'app-sales-invoices',
  templateUrl: './sales-invoices.component.html',
  styleUrl:    './sales-invoices.component.css',
  imports:     [SharedModule, SalesInvoiceEntryDrawerComponent],
})
export class SalesInvoicesComponent implements OnInit {

  // ── List ──────────────────────────────────────────────────────────────────
  invoices:   SalesInvoiceDto[] = [];
  totalCount  = 0;
  pageIndex   = 1;
  pageSize    = 10;
  filter      = '';
  loading     = false;
  activeTab: TabKey = 'all';

  stats: InvoiceStats = { total: 0, draft: 0, issued: 0, paid: 0, overdue: 0 };

  tabs: { key: TabKey; label: string; countKey: StatsKey }[] = [
    { key: 'all',     label: 'All',     countKey: 'total'   },
    { key: 'draft',   label: 'Draft',   countKey: 'draft'   },
    { key: 'issued',  label: 'Issued',  countKey: 'issued'  },
    { key: 'paid',    label: 'Paid',    countKey: 'paid'    },
    { key: 'overdue', label: 'Overdue', countKey: 'overdue' },
  ];

  // ── Drawer ────────────────────────────────────────────────────────────────
  isDrawerOpen                   = false;
  selectedInvoice: SalesInvoiceDto | null = null;
  selectedItems:   SalesInvoiceItemDto[]  = [];

  deletingIds = new Set<number>();

  statusLabels = SalesInvoiceStatusLabels;
  statusColors = SalesInvoiceStatusColors;

  constructor(
    private invoiceService: SalesInvoiceService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  // ── Drawer ────────────────────────────────────────────────────────────────
  openDrawerForCreate(): void {
    this.selectedInvoice = null;
    this.selectedItems   = [];
    this.isDrawerOpen    = true;
  }

  openDrawerForEdit(item: SalesInvoiceDto): void {
    this.selectedInvoice = item;
    this.isDrawerOpen    = true;
    // Load line items for the selected invoice
    this.invoiceService.getItems(item.id).subscribe(items => {
      this.selectedItems = items;
    });
  }

  onDrawerClosed(): void { this.isDrawerOpen = false; this.selectedItems = []; }

  onInvoiceSaved(): void {
    this.isDrawerOpen  = false;
    this.selectedItems = [];
    this.loadStats();
    this.loadData();
  }

  // ── List ──────────────────────────────────────────────────────────────────
  loadStats(): void {
    forkJoin({
      all:     this.invoiceService.getListData({ maxResultCount: 1, skipCount: 0 }),
      draft:   this.invoiceService.getListData({ status: 1, maxResultCount: 1, skipCount: 0 }),
      issued:  this.invoiceService.getListData({ status: 2, maxResultCount: 1, skipCount: 0 }),
      paid:    this.invoiceService.getListData({ status: 3, maxResultCount: 1, skipCount: 0 }),
      overdue: this.invoiceService.getListData({ status: 5, maxResultCount: 1, skipCount: 0 }),
    }).subscribe(r => {
      this.stats.total   = r.all.totalCount;
      this.stats.draft   = r.draft.totalCount;
      this.stats.issued  = r.issued.totalCount;
      this.stats.paid    = r.paid.totalCount;
      this.stats.overdue = r.overdue.totalCount;
    });
  }

  loadData(): void {
    this.loading = true;
    const params: any = {
      filter:         this.filter || undefined,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };
    if (this.activeTab === 'draft')   params.status = 1;
    if (this.activeTab === 'issued')  params.status = 2;
    if (this.activeTab === 'paid')    params.status = 3;
    if (this.activeTab === 'overdue') params.status = 5;

    this.invoiceService.getListData(params).subscribe({
      next: result => {
        this.invoices   = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
  }

  // ── Delete ────────────────────────────────────────────────────────────────
  deleteInvoice(id: number): void {
    this.deletingIds.add(id);
    this.invoiceService.delete(id).subscribe({
      next: () => {
        this.message.success('Invoice deleted.');
        this.deletingIds.delete(id);
        this.loadStats();
        this.loadData();
      },
      error: () => {
        this.message.error('Failed to delete invoice.');
        this.deletingIds.delete(id);
      },
    });
  }

  selectTab(tab: TabKey): void  { this.activeTab = tab; this.pageIndex = 1; this.loadData(); }
  onSearch(): void               { this.pageIndex = 1; this.loadData(); }
  resetFilters(): void           { this.filter = ''; this.activeTab = 'all'; this.pageIndex = 1; this.loadData(); }
  onPageIndexChange(i: number)   { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)    { this.pageSize = s; this.pageIndex = 1; this.loadData(); }
}

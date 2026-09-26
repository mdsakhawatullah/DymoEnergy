import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { OrderService } from '../../proxy/orders/order.service';
import {
  OrderDto,
  OrderItemDto,
  OrderStatusLabels,
  OrderPriorityLabels,
} from '../../proxy/orders/models';
import { OrderEntryDrawerComponent } from './entry-drawer/order-entry-drawer.component';

type TabKey = 'all' | 'pending' | 'confirmed' | 'processing' | 'shipped' | 'delivered' | 'cancelled';
type DateKey = 'any' | '30' | '90' | 'month';

interface OrderStats {
  total: number; pending: number; confirmed: number;
  processing: number; shipped: number; delivered: number; cancelled: number;
}

/** Status id → pill palette, per the design's status colour map. */
const STATUS_PILL: Record<number, string> = {
  1: 'pending', 2: 'confirmed', 3: 'processing', 4: 'pending',
  5: 'shipped', 6: 'success', 7: 'neutral', 8: 'processing', 9: 'danger',
};

/** Priority id → tone + glyph (dot, triangle, warning). */
const PRIORITY: Record<number, { tone: string; icon: string }> = {
  1: { tone: 'normal', icon: 'M12 9a3 3 0 1 0 0 6a3 3 0 1 0 0-6' },
  2: { tone: 'normal', icon: 'M12 9a3 3 0 1 0 0 6a3 3 0 1 0 0-6' },
  3: { tone: 'high',   icon: 'M12 5l7 9H5z' },
  4: { tone: 'urgent', icon: 'M12 3l9 16H3zM11 10h2v5h-2zM11 16h2v2h-2z' },
};

@Component({
  selector:    'app-orders',
  templateUrl: './orders.component.html',
  styleUrl:    './orders.component.css',
  imports:     [SharedModule, OrderEntryDrawerComponent],
})
export class OrdersComponent implements OnInit {

  // ── List ──────────────────────────────────────────────────────────────────
  orders:    OrderDto[] = [];
  totalCount = 0;
  pageIndex  = 1;
  pageSize   = 10;
  filter     = '';
  loading    = false;
  activeTab: TabKey = 'all';

  // ── Filters ───────────────────────────────────────────────────────────────
  dateRange: DateKey = 'any';
  paidState: number | null = null;
  priority:  number | null = null;

  dateOptions: { key: DateKey; label: string }[] = [
    { key: 'any',   label: 'Any date'    },
    { key: '30',    label: 'Last 30 days' },
    { key: '90',    label: 'Last 90 days' },
    { key: 'month', label: 'This month'   },
  ];

  paidOptions = [
    { value: 1, label: 'Paid in full' },
    { value: 2, label: 'Partly paid'  },
    { value: 3, label: 'Unpaid'       },
  ];

  priorityOptions = [
    { value: 1, label: 'Low'    },
    { value: 2, label: 'Normal' },
    { value: 3, label: 'High'   },
    { value: 4, label: 'Urgent' },
  ];

  stats: OrderStats = {
    total: 0, pending: 0, confirmed: 0, processing: 0,
    shipped: 0, delivered: 0, cancelled: 0,
  };

  monthCount     = 0;
  outstanding    = 0;
  outstandingOf  = 0;
  oldestPending: string | null = null;

  tabs: { key: TabKey; label: string; status?: number }[] = [
    { key: 'all',        label: 'All'        },
    { key: 'pending',    label: 'Pending',    status: 1 },
    { key: 'confirmed',  label: 'Confirmed',  status: 2 },
    { key: 'processing', label: 'Processing', status: 3 },
    { key: 'shipped',    label: 'Shipped',    status: 5 },
    { key: 'delivered',  label: 'Delivered',  status: 6 },
    { key: 'cancelled',  label: 'Cancelled',  status: 7 },
  ];

  // ── Drawer ────────────────────────────────────────────────────────────────
  isDrawerOpen                = false;
  selectedOrder: OrderDto | null = null;
  selectedItems: OrderItemDto[]  = [];

  deletingIds = new Set<number>();

  statusLabels   = OrderStatusLabels;
  priorityLabels = OrderPriorityLabels;

  constructor(
    private orderSvc: OrderService,
    private message:  NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadData();
  }

  // ── Drawer ────────────────────────────────────────────────────────────────
  openDrawerForCreate(): void {
    this.selectedOrder = null;
    this.selectedItems = [];
    this.isDrawerOpen  = true;
  }

  openDrawerForEdit(order: OrderDto): void {
    this.selectedOrder = order;
    this.isDrawerOpen  = true;
    this.orderSvc.getItems(order.id).subscribe(items => {
      this.selectedItems = items;
    });
  }

  onDrawerClosed(): void  { this.isDrawerOpen = false; this.selectedItems = []; }

  onOrderSaved(): void {
    this.isDrawerOpen  = false;
    this.selectedItems = [];
    this.loadStats();
    this.loadData();
  }

  // ── Data ──────────────────────────────────────────────────────────────────
  loadStats(): void {
    const monthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString();

    forkJoin({
      all:        this.orderSvc.getListData({ maxResultCount: 1, skipCount: 0 }),
      pending:    this.orderSvc.getListData({ status: 1, maxResultCount: 1, skipCount: 0 }),
      confirmed:  this.orderSvc.getListData({ status: 2, maxResultCount: 1, skipCount: 0 }),
      processing: this.orderSvc.getListData({ status: 3, maxResultCount: 1, skipCount: 0 }),
      shipped:    this.orderSvc.getListData({ status: 5, maxResultCount: 1, skipCount: 0 }),
      delivered:  this.orderSvc.getListData({ status: 6, maxResultCount: 1, skipCount: 0 }),
      cancelled:  this.orderSvc.getListData({ status: 7, maxResultCount: 1, skipCount: 0 }),
      month:      this.orderSvc.getListData({ dateFrom: monthStart, maxResultCount: 1, skipCount: 0 }),
      // Oldest unconfirmed order, for the "Oldest: …" note
      oldest:     this.orderSvc.getListData({ status: 1, sorting: 'orderDate asc', maxResultCount: 1, skipCount: 0 }),
      // Everything still carrying a balance, capped — see `outstanding` note
      dueFull:    this.orderSvc.getListData({ maxResultCount: 500, skipCount: 0 }),
    }).subscribe(r => {
      this.stats.total      = r.all.totalCount;
      this.stats.pending    = r.pending.totalCount;
      this.stats.confirmed  = r.confirmed.totalCount;
      this.stats.processing = r.processing.totalCount;
      this.stats.shipped    = r.shipped.totalCount;
      this.stats.delivered  = r.delivered.totalCount;
      this.stats.cancelled  = r.cancelled.totalCount;
      this.monthCount       = r.month.totalCount;
      this.oldestPending    = r.oldest.items[0]?.orderDate ?? null;

      const due = r.dueFull.items.filter(o => (o.balanceDue || 0) > 0);
      this.outstanding   = due.reduce((sum, o) => sum + (o.balanceDue || 0), 0);
      this.outstandingOf = due.length;
    });
  }

  loadData(): void {
    this.loading = true;
    const activeTab = this.tabs.find(t => t.key === this.activeTab);

    this.orderSvc.getListData({
      filter:         this.filter || undefined,
      status:         activeTab?.status,
      priority:       this.priority ?? undefined,
      paidState:      this.paidState ?? undefined,
      dateFrom:       this.dateFrom() ?? undefined,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    }).subscribe({
      next: result => {
        this.orders     = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
  }

  private dateFrom(): string | null {
    const now = new Date();
    if (this.dateRange === '30')    return new Date(now.getTime() - 30 * 864e5).toISOString();
    if (this.dateRange === '90')    return new Date(now.getTime() - 90 * 864e5).toISOString();
    if (this.dateRange === 'month') return new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
    return null;
  }

  deleteOrder(id: number): void {
    this.deletingIds.add(id);
    this.orderSvc.delete(id).subscribe({
      next: () => {
        this.message.success('Order deleted.');
        this.deletingIds.delete(id);
        this.loadStats();
        this.loadData();
      },
      error: () => {
        this.message.error('Failed to delete order.');
        this.deletingIds.delete(id);
      },
    });
  }

  // ── Row helpers ───────────────────────────────────────────────────────────
  statusPill(status: number): string { return STATUS_PILL[status] ?? 'neutral'; }
  priorityTone(p: number): string    { return PRIORITY[p]?.tone ?? 'normal'; }
  priorityIcon(p: number): string    { return PRIORITY[p]?.icon ?? PRIORITY[1].icon; }

  /** Fraction of the grand total already settled, 0–1. */
  paidFraction(o: OrderDto): number {
    const total = o.grandTotal || 0;
    if (total <= 0) return 1;
    return Math.min(1, Math.max(0, (total - (o.balanceDue || 0)) / total));
  }

  paidPercent(o: OrderDto): number { return Math.round(this.paidFraction(o) * 100); }

  paidTone(o: OrderDto): string {
    const f = this.paidFraction(o);
    if (f >= 1) return 'paid';
    return (o.balanceDue || 0) >= (o.grandTotal || 0) ? 'unpaid' : 'partial';
  }

  paidLabel(o: OrderDto): string {
    const f = this.paidFraction(o);
    if (f >= 1) return 'Paid in full';
    const due = this.money(o.balanceDue || 0);
    return f <= 0 ? `Unpaid · BDT ${due} due` : `${this.paidPercent(o)}% paid · BDT ${due} due`;
  }

  /** Indian/Bangladeshi digit grouping: 6,85,000. */
  money(n: number): string {
    return Math.round(n || 0).toLocaleString('en-IN');
  }

  /** Lakh-compact for the KPI headline. */
  compact(n: number): string {
    return n >= 100000 ? (n / 100000).toFixed(2) + 'L' : this.money(n);
  }

  daysAgo(iso: string | null): string {
    if (!iso) return '—';
    const days = Math.floor((Date.now() - new Date(iso).getTime()) / 864e5);
    if (days <= 0) return 'today';
    return days === 1 ? '1 day ago' : `${days} days ago`;
  }

  get hasFilters(): boolean {
    return !!this.filter || this.activeTab !== 'all' || this.dateRange !== 'any'
        || this.paidState !== null || this.priority !== null;
  }

  get showingLabel(): string {
    const n = this.orders.length;
    return `Showing ${n} of ${this.totalCount} ${this.totalCount === 1 ? 'order' : 'orders'}`;
  }

  selectTab(key: TabKey): void { this.activeTab = key; this.pageIndex = 1; this.loadData(); }
  onSearch(): void             { this.pageIndex = 1; this.loadData(); }
  onFilterChange(): void       { this.pageIndex = 1; this.loadData(); }

  resetFilters(): void {
    this.filter = '';
    this.activeTab = 'all';
    this.dateRange = 'any';
    this.paidState = null;
    this.priority = null;
    this.pageIndex = 1;
    this.loadData();
  }

  onPageIndexChange(i: number) { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)  { this.pageSize = s; this.pageIndex = 1; this.loadData(); }

  tabCount(key: TabKey): number {
    return this.stats[key as keyof OrderStats] ?? 0;
  }
}

import { Component, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { OrderService } from '../../proxy/orders/order.service';
import {
  OrderDto,
  OrderItemDto,
  OrderStatusLabels,
  OrderStatusColors,
  OrderStageLabels,
  OrderStageColors,
  OrderPriorityLabels,
  OrderPriorityColors,
} from '../../proxy/orders/models';
import { OrderEntryDrawerComponent } from './entry-drawer/order-entry-drawer.component';

type TabKey = 'all' | 'pending' | 'confirmed' | 'processing' | 'shipped' | 'delivered' | 'cancelled';

interface OrderStats {
  total: number; pending: number; confirmed: number;
  processing: number; shipped: number; delivered: number; cancelled: number;
}

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

  stats: OrderStats = {
    total: 0, pending: 0, confirmed: 0, processing: 0,
    shipped: 0, delivered: 0, cancelled: 0,
  };

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

  // ── Label / colour maps ───────────────────────────────────────────────────
  statusLabels   = OrderStatusLabels;
  statusColors   = OrderStatusColors;
  stageLabels    = OrderStageLabels;
  stageColors    = OrderStageColors;
  priorityLabels = OrderPriorityLabels;
  priorityColors = OrderPriorityColors;

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
    forkJoin({
      all:        this.orderSvc.getListData({ maxResultCount: 1, skipCount: 0 }),
      pending:    this.orderSvc.getListData({ status: 1, maxResultCount: 1, skipCount: 0 }),
      confirmed:  this.orderSvc.getListData({ status: 2, maxResultCount: 1, skipCount: 0 }),
      processing: this.orderSvc.getListData({ status: 3, maxResultCount: 1, skipCount: 0 }),
      shipped:    this.orderSvc.getListData({ status: 5, maxResultCount: 1, skipCount: 0 }),
      delivered:  this.orderSvc.getListData({ status: 6, maxResultCount: 1, skipCount: 0 }),
      cancelled:  this.orderSvc.getListData({ status: 7, maxResultCount: 1, skipCount: 0 }),
    }).subscribe(r => {
      this.stats.total      = r.all.totalCount;
      this.stats.pending    = r.pending.totalCount;
      this.stats.confirmed  = r.confirmed.totalCount;
      this.stats.processing = r.processing.totalCount;
      this.stats.shipped    = r.shipped.totalCount;
      this.stats.delivered  = r.delivered.totalCount;
      this.stats.cancelled  = r.cancelled.totalCount;
    });
  }

  loadData(): void {
    this.loading = true;
    const activeTab = this.tabs.find(t => t.key === this.activeTab);
    const params: any = {
      filter:         this.filter || undefined,
      status:         activeTab?.status,
      skipCount:      (this.pageIndex - 1) * this.pageSize,
      maxResultCount: this.pageSize,
    };

    this.orderSvc.getListData(params).subscribe({
      next: result => {
        this.orders     = result.items;
        this.totalCount = result.totalCount;
        this.loading    = false;
      },
      error: () => { this.loading = false; },
    });
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

  selectTab(key: TabKey): void  { this.activeTab = key; this.pageIndex = 1; this.loadData(); }
  onSearch(): void               { this.pageIndex = 1; this.loadData(); }
  resetFilters(): void           { this.filter = ''; this.activeTab = 'all'; this.pageIndex = 1; this.loadData(); }
  onPageIndexChange(i: number)   { this.pageIndex = i; this.loadData(); }
  onPageSizeChange(s: number)    { this.pageSize = s; this.pageIndex = 1; this.loadData(); }

  tabCount(key: TabKey): number {
    return this.stats[key as keyof OrderStats] ?? 0;
  }
}

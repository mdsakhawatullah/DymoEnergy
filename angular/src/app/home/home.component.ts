import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ConfigStateService } from '@abp/ng.core';
import { Observable, catchError, forkJoin, map, of } from 'rxjs';

import { OrderService } from '../proxy/orders/order.service';
import { QuoteRequestService } from '../proxy/quote-requests/quote-request.service';
import { ProductService } from '../proxy/products/product.service';
import { CatalogueService } from '../proxy/catalogues/catalogue.service';
import { OrderDto, OrderStatusLabels } from '../proxy/orders/models';
import { QuoteRequestDto, QuoteRequestStatusLabels } from '../proxy/quote-requests/models';
import { CatalogueDto } from '../proxy/catalogues/models';
import { ProductDto } from '../proxy/products/models';

type OrderTab = 'pending' | 'progress' | 'delivered';

/** Dot colour per order status id — see OrderStatusLabels. */
const ORDER_DOT: Record<number, string> = {
  1: 'amber', 2: 'blue', 3: 'blue', 4: 'amber', 5: 'purple',
  6: 'green', 7: '', 8: 'purple', 9: 'red',
};

const QUOTE_DOT: Record<number, string> = { 1: 'blue', 2: 'amber', 3: '' };

/** Stock at or below this is surfaced in the Low stock panel. */
const LOW_STOCK_THRESHOLD = 10;

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
  imports: [RouterLink, DatePipe, DecimalPipe],
})
export class HomeComponent implements OnInit {
  private orderService     = inject(OrderService);
  private quoteService     = inject(QuoteRequestService);
  private productService   = inject(ProductService);
  private catalogueService = inject(CatalogueService);
  private config           = inject(ConfigStateService);

  today = new Date();

  // ── Stats ───────────────────────────────────────────────────────────────────
  ordersTotal   = signal(0);
  ordersPending = signal(0);
  quotesTotal   = signal(0);
  quotesNew     = signal(0);
  productsTotal = signal(0);
  monthOrders   = signal(0);
  monthRevenue  = signal(0);

  // ── Lists ───────────────────────────────────────────────────────────────────
  orders     = signal<OrderDto[]>([]);
  quotes     = signal<QuoteRequestDto[]>([]);
  catalogues = signal<CatalogueDto[]>([]);
  products   = signal<ProductDto[]>([]);

  loading        = signal(true);
  quotesLoading  = signal(true);
  productsLoading = signal(true);
  quotesDenied   = signal(false);

  activeTab = signal<OrderTab>('pending');

  greeting = computed(() => {
    const h = this.today.getHours();
    if (h < 12) return 'Good morning';
    if (h < 18) return 'Good afternoon';
    return 'Good evening';
  });

  userName = computed(() => {
    const user = this.config.getOne('currentUser');
    return user?.name || user?.userName || 'there';
  });

  // ── Recent orders, bucketed like the spec's tabs ─────────────────────────────
  pendingOrders   = computed(() => this.orders().filter(o => o.status === 1));
  progressOrders  = computed(() => this.orders().filter(o => o.status > 1 && o.status < 6));
  deliveredOrders = computed(() => this.orders().filter(o => o.status === 6));

  visibleOrders = computed(() => {
    const tab = this.activeTab();
    if (tab === 'pending')  return this.pendingOrders();
    if (tab === 'progress') return this.progressOrders();
    return this.deliveredOrders();
  });

  /** Quote requests bucketed by status, empty groups dropped. */
  quoteGroups = computed(() =>
    [1, 2].map(status => ({
      status,
      label: QuoteRequestStatusLabels[status],
      items: this.quotes().filter(q => q.status === status),
    })).filter(g => g.items.length)
  );

  /** Products running low, scarcest first. */
  lowStock = computed(() =>
    this.products()
      .filter(p => p.stockQuantity <= LOW_STOCK_THRESHOLD)
      .sort((a, b) => a.stockQuantity - b.stockQuantity)
      .slice(0, 5)
  );

  /** Catalogue names by id, for the product table's category column. */
  private catalogueName = computed(() => {
    const map = new Map<number, string>();
    this.catalogues().forEach(c => map.set(c.id, c.name || 'Uncategorised'));
    return map;
  });

  /** Highest-value products, standing in for the spec's "top selling" table. */
  topProducts = computed(() =>
    [...this.products()]
      .sort((a, b) => (b.discountPrice ?? b.price) - (a.discountPrice ?? a.price))
      .slice(0, 5)
      .map(p => ({ product: p, catalogue: this.catalogueName().get(p.catalogueId) || '—' }))
  );

  ngOnInit(): void {
    const monthStart = new Date(this.today.getFullYear(), this.today.getMonth(), 1).toISOString();

    // Each widget degrades on its own: an endpoint the user lacks permission
    // for shows a dash instead of taking the whole dashboard down.
    forkJoin({
      ordersAll:   this.safe(this.orderService.getListData({ maxResultCount: 1, skipCount: 0 })),
      ordersPend:  this.safe(this.orderService.getListData({ status: 1, maxResultCount: 1, skipCount: 0 })),
      quotesAll:   this.safe(this.quoteService.getListData({ maxResultCount: 1, skipCount: 0 })),
      quotesNew:   this.safe(this.quoteService.getListData({ status: 1, maxResultCount: 1, skipCount: 0 })),
      productsAll: this.safe(this.productService.getListData({ maxResultCount: 1, skipCount: 0 })),
      thisMonth:   this.safe(this.orderService.getListData({ dateFrom: monthStart, maxResultCount: 500, skipCount: 0 })),
    }).subscribe(r => {
      this.ordersTotal.set(r.ordersAll.totalCount);
      this.ordersPending.set(r.ordersPend.totalCount);
      this.quotesTotal.set(r.quotesAll.totalCount);
      this.quotesNew.set(r.quotesNew.totalCount);
      this.productsTotal.set(r.productsAll.totalCount);
      this.quotesDenied.set(r.quotesAll.denied);

      this.monthOrders.set(r.thisMonth.totalCount);
      this.monthRevenue.set(r.thisMonth.items.reduce((sum, o) => sum + (o.grandTotal || 0), 0));
    });

    this.safe(this.orderService.getListData({ maxResultCount: 12, skipCount: 0 })).subscribe(r => {
      this.orders.set(r.items);
      // Land on the first tab that actually has something in it
      if (!this.pendingOrders().length) {
        this.activeTab.set(this.progressOrders().length ? 'progress' : 'delivered');
      }
      this.loading.set(false);
    });

    this.safe(this.quoteService.getListData({ maxResultCount: 6, skipCount: 0 })).subscribe(r => {
      this.quotes.set(r.items);
      this.quotesDenied.set(r.denied);
      this.quotesLoading.set(false);
    });

    forkJoin({
      catalogues: this.safe(this.catalogueService.getListData({ maxResultCount: 50, skipCount: 0 })),
      products:   this.safe(this.productService.getListData({ maxResultCount: 500, skipCount: 0 })),
    }).subscribe(r => {
      this.catalogues.set(r.catalogues.items);
      this.products.set(r.products.items);
      this.productsLoading.set(false);
    });
  }

  selectTab(tab: OrderTab): void { this.activeTab.set(tab); }

  orderDot(status: number): string   { return ORDER_DOT[status] ?? ''; }
  orderLabel(status: number): string { return OrderStatusLabels[status] ?? '—'; }
  quoteDot(status: number): string   { return QUOTE_DOT[status] ?? ''; }

  initials(name?: string): string {
    if (!name) return '—';
    return name.trim().split(/\s+/).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  }

  /** Lakh-compact for headline figures, e.g. 685000 → "6.85L". */
  compact(amount: number): string {
    if (amount >= 100000) return (amount / 100000).toFixed(2) + 'L';
    return amount.toLocaleString('en-IN');
  }

  /** Never let one rejected endpoint reject the whole forkJoin. */
  private safe<T>(source: Observable<{ totalCount: number; items: T[] }>) {
    return source.pipe(
      catchError(() => of({ totalCount: 0, items: [] as T[], denied: true })),
      map(r => ({ totalCount: r.totalCount, items: r.items, denied: (r as any).denied === true })),
    );
  }
}

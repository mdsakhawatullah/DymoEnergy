import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { catchError, of } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { OrderService } from '../../../proxy/orders/order.service';
import {
  OrderDto,
  OrderItemDto,
  OrderStatusLabels,
  OrderStatusColors,
  OrderStageLabels,
  OrderStageColors,
  OrderPaymentTypeLabels,
} from '../../../proxy/orders/models';
import { OrderEntryDrawerComponent } from '../entry-drawer/order-entry-drawer.component';
import { AdminSiteSettingService } from '../../../proxy/admin-site-settings/admin-site-setting.service';
import { AdminSiteSettingDto } from '../../../proxy/admin-site-settings/models';

@Component({
  selector:    'app-order-invoice',
  templateUrl: './order-invoice.component.html',
  styleUrl:    './order-invoice.component.css',
  imports:     [SharedModule, OrderEntryDrawerComponent],
})
export class OrderInvoiceComponent implements OnInit {

  orderId      = 0;
  order: OrderDto | null       = null;
  items: OrderItemDto[]        = [];
  siteSettings: AdminSiteSettingDto | null = null;
  loading      = true;
  notFound     = false;
  isDrawerOpen = false;

  // ── Label maps ────────────────────────────────────────────────────────────────
  statusLabels      = OrderStatusLabels;
  statusColors      = OrderStatusColors;
  stageLabels       = OrderStageLabels;
  stageColors       = OrderStageColors;
  paymentTypeLabels = OrderPaymentTypeLabels;

  // Stage stepper: New → Processing → Ready to Ship → In Transit → Delivered
  get currentStep(): number {
    const s = this.order?.stage ?? 1;
    if (s <= 1) return 0;
    if (s <= 3) return 1;
    if (s <= 4) return 2;
    if (s <= 6) return 3;
    return 4;
  }

  /** Cancelled / Refunded / Returned */
  get isSpecialStatus(): boolean {
    return [7, 8, 9].includes(this.order?.status ?? 0);
  }

  get customerInitials(): string {
    return (this.order?.customerName ?? '')
      .split(' ')
      .filter(w => w.length > 0)
      .map(w => w[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }

  get companyAddressLine(): string {
    const s = this.siteSettings;
    if (!s) return '';
    return [s.address, s.city, s.state, s.zipCode, s.country]
      .filter(v => !!v)
      .join(', ');
  }

  get isFullyPaid(): boolean {
    return (this.order?.balanceDue ?? 1) <= 0;
  }

  get outstanding(): number {
    return Math.max(0, this.order?.balanceDue ?? 0);
  }

  constructor(
    private route:          ActivatedRoute,
    private router:         Router,
    private orderSvc:       OrderService,
    private siteSettingSvc: AdminSiteSettingService,
    private message:        NzMessageService,
  ) {}

  ngOnInit(): void {
    this.orderId = +this.route.snapshot.paramMap.get('id')!;
    if (!this.orderId) { this.router.navigate(['/orders']); return; }
    this.loadData();
  }

  loadData(): void {
    this.loading  = true;
    this.notFound = false;
    forkJoin({
      order:        this.orderSvc.get(this.orderId).pipe(catchError(() => of(null))),
      items:        this.orderSvc.getItems(this.orderId).pipe(catchError(() => of([]))),
      siteSettings: this.siteSettingSvc.getActive().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ order, items, siteSettings }) => {
        if (!order) { this.notFound = true; this.loading = false; return; }
        this.order        = order as OrderDto;
        this.items        = (items as OrderItemDto[]).sort((a, b) => a.displayOrder - b.displayOrder);
        this.siteSettings = siteSettings;
        this.loading      = false;
      },
      error: () => { this.notFound = true; this.loading = false; },
    });
  }

  openEditDrawer(): void { this.isDrawerOpen = true; }
  onDrawerClosed(): void { this.isDrawerOpen = false; }
  onOrderSaved():  void  { this.isDrawerOpen = false; this.loadData(); }

  goBack(): void { this.router.navigate(['/orders']); }

  print(): void {
    const src = document.getElementById('print-invoice');
    if (!src) { window.print(); return; }

    const clone = src.cloneNode(true) as HTMLElement;
    clone.id = 'print-invoice-clone';
    document.body.appendChild(clone);
    document.body.classList.add('invoice-print-mode');

    window.addEventListener('afterprint', () => {
      document.body.classList.remove('invoice-print-mode');
      clone.remove();
    }, { once: true });

    window.print();
  }

  fmtDate(d?: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  fmtDateLong(d?: string | null): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'long', year: 'numeric' });
  }

  fmtDateTime(d?: string | null): string {
    if (!d) return '—';
    const dt   = new Date(d);
    const date = dt.toLocaleDateString('en-GB',  { day: '2-digit', month: 'short', year: 'numeric' });
    const time = dt.toLocaleTimeString('en-GB',  { hour: '2-digit', minute: '2-digit' });
    return `${date}, ${time}`;
  }
}

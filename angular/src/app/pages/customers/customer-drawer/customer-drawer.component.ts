import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { CustomerService } from '../../../proxy/customers/customer.service';
import { CUSTOMER_SOURCES, CUSTOMER_TYPES, CustomerDto, CustomerStatus, CustomerType } from '../../../proxy/customers/models';
import { fmtFull } from '../../budgets-costs/finance.utils';

@Component({
  selector: 'app-customer-drawer',
  templateUrl: './customer-drawer.component.html',
  styleUrls: ['./customer-drawer.component.css'],
  imports: [SharedModule],
})
export class CustomerDrawerComponent implements OnChanges {
  @Input({ required: true }) customerId!: number;
  @Input() canEdit = false;
  @Input() canDelete = false;
  @Output() closed = new EventEmitter<void>();
  @Output() edit = new EventEmitter<number>();
  /** Something was linked or deleted, so the list behind should reload. */
  @Output() changed = new EventEmitter<void>();

  readonly Type = CustomerType;
  readonly Status = CustomerStatus;

  customer: CustomerDto | null = null;
  failed = false;
  busy = false;
  tab: 'orders' | 'invoices' | 'quotes' = 'orders';

  money = (v: number | null | undefined) => fmtFull(v ?? 0, '৳');

  constructor(private api: CustomerService, private message: NzMessageService, private router: Router) {}

  ngOnChanges(): void {
    this.load();
  }

  load(): void {
    this.customer = null;
    this.failed = false;
    this.api.get(this.customerId).subscribe({
      next: c => (this.customer = c),
      error: () => (this.failed = true),
    });
  }

  get initials(): string {
    return (this.customer?.name || '?').split(' ').filter(Boolean).slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  get typeLabel(): string {
    return CUSTOMER_TYPES.find(t => t.value === this.customer?.type)?.label ?? '';
  }

  get sourceLabel(): string {
    return CUSTOMER_SOURCES.find(s => s.value === this.customer?.source)?.label ?? '';
  }

  get whereText(): string {
    const c = this.customer;
    if (!c) return '';
    return [c.address, c.area, c.city, c.district].filter(Boolean).join(', ');
  }

  get quietText(): string {
    const d = this.customer?.quietDays;
    if (d == null) return 'has never ordered';
    if (d === 0) return 'ordered today';
    if (d === 1) return 'ordered yesterday';
    if (d < 31) return `last ordered ${d} days ago`;
    const months = Math.round(d / 30);
    return months < 12 ? `last ordered ${months} ${months === 1 ? 'month' : 'months'} ago` : `last ordered ${Math.round(d / 365)} years ago`;
  }

  /** Attaches old orders that carry this phone number but were never linked. */
  linkMatching(): void {
    this.busy = true;
    this.api.linkMatching(this.customerId).subscribe({
      next: r => {
        this.busy = false;
        const bits: string[] = [];
        if (r.orders) bits.push(`${r.orders} ${r.orders === 1 ? 'order' : 'orders'}`);
        if (r.invoices) bits.push(`${r.invoices} ${r.invoices === 1 ? 'invoice' : 'invoices'}`);
        this.message.success(bits.length ? `Attached ${bits.join(' and ')}.` : 'Nothing more to attach.');
        this.load();
        this.changed.emit();
      },
      error: () => (this.busy = false),
    });
  }

  openOrder(id: number): void {
    this.router.navigate(['/orders'], { queryParams: { highlight: id } });
    this.closed.emit();
  }

  openInvoice(id: number): void {
    window.open(`/orders/${id}/invoice`, '_blank', 'noopener');
  }

  remove(): void {
    this.busy = true;
    this.api.delete(this.customerId).subscribe({
      next: () => {
        this.busy = false;
        this.message.success(`${this.customer?.name} removed. Their orders and invoices were kept.`);
        this.changed.emit();
        this.closed.emit();
      },
      error: () => (this.busy = false),
    });
  }
}

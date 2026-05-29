import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import {
  SalesInvoiceDto,
  SalesInvoiceItemDto,
  SalesInvoiceStatusLabels,
  SalesInvoiceStatusColors,
  SalesInvoicePaymentMethodLabels,
} from '../../../proxy/sales-invoices/models';
import { SalesInvoiceEntryDrawerComponent } from '../entry-drawer/sales-invoice-entry-drawer.component';

@Component({
  selector:    'app-invoice-details',
  templateUrl: './invoice-details.component.html',
  styleUrl:    './invoice-details.component.css',
  imports:     [SharedModule, SalesInvoiceEntryDrawerComponent],
})
export class InvoiceDetailsComponent implements OnInit {

  invoiceId = 0;
  invoice: SalesInvoiceDto | null = null;
  items: SalesInvoiceItemDto[] = [];
  loading = true;

  isDrawerOpen = false;

  statusLabels        = SalesInvoiceStatusLabels;
  statusColors        = SalesInvoiceStatusColors;
  paymentMethodLabels = SalesInvoicePaymentMethodLabels;

  // Step flow: Draft(0) → Issued(1) → Partially Paid(2) → Paid(3)
  get currentStep(): number {
    switch (this.invoice?.status) {
      case 1:  return 0;
      case 2:  return 1;
      case 5:  return 1; // Overdue stays at Issued step
      case 4:  return 2;
      case 3:  return 3;
      default: return 0;
    }
  }

  get isSpecialStatus(): boolean {
    return [5, 6, 7].includes(this.invoice?.status ?? 0);
  }

  get customerInitials(): string {
    const name = this.invoice?.customerName ?? '';
    return name.split(' ')
      .filter(w => w.length > 0)
      .map(w => w[0].toUpperCase())
      .slice(0, 2)
      .join('');
  }

  get isFullyPaid(): boolean {
    return (this.invoice?.balanceDue ?? 1) <= 0;
  }

  get outstanding(): number {
    return Math.max(0, this.invoice?.balanceDue ?? 0);
  }

  constructor(
    private route:          ActivatedRoute,
    private router:         Router,
    private invoiceService: SalesInvoiceService,
    private message:        NzMessageService,
  ) {}

  ngOnInit(): void {
    this.invoiceId = +this.route.snapshot.paramMap.get('id')!;
    if (!this.invoiceId) { this.router.navigate(['/sales-invoices']); return; }
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    forkJoin({
      invoice: this.invoiceService.get(this.invoiceId),
      items:   this.invoiceService.getItems(this.invoiceId),
    }).subscribe({
      next: ({ invoice, items }) => {
        this.invoice = invoice;
        this.items   = items.sort((a, b) => a.displayOrder - b.displayOrder);
        this.loading = false;
      },
      error: () => {
        this.message.error('Invoice not found.');
        this.loading = false;
      },
    });
  }

  openEditDrawer(): void { this.isDrawerOpen = true; }
  onDrawerClosed(): void { this.isDrawerOpen = false; }
  onInvoiceSaved(): void { this.isDrawerOpen = false; this.loadData(); }

  goBack(): void { this.router.navigate(['/sales-invoices']); }

  print(): void {
    const src = document.getElementById('print-invoice');
    if (!src) { window.print(); return; }

    // Clone the invoice into <body> so we can display:none everything else
    // (visibility:hidden keeps space → blank second page; this approach avoids that)
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
    const dt = new Date(d);
    const date = dt.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    const time = dt.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });
    return `${date}, ${time}`;
  }
}

import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import {
  SalesInvoiceDto,
  SalesInvoiceItemDto,
  SalesInvoicePaymentDto,
  SalesInvoicePaymentMethodLabels,
  SalesInvoiceShortStatus,
} from '../../../proxy/sales-invoices/models';
import { SalesInvoiceEntryDrawerComponent } from '../entry-drawer/sales-invoice-entry-drawer.component';
import { AdminSiteSettingService } from '../../../proxy/admin-site-settings/admin-site-setting.service';
import { AdminSiteSettingDto } from '../../../proxy/admin-site-settings/models';
import { TakaPipe, describeSerials, formatTaka } from '../invoice-format';

/**
 * Printable A4 invoice. Opened in a new tab from the quick view:
 *   /sales-invoices/:id?print=1  → opens the print dialog once loaded
 *   /sales-invoices/:id?pdf=1    → same, with the tab titled after the invoice so
 *                                  "Save as PDF" suggests INV-2026-00987.pdf
 */
@Component({
  selector:    'app-invoice-details',
  templateUrl: './invoice-details.component.html',
  styleUrl:    './invoice-details.component.css',
  imports:     [SharedModule, SalesInvoiceEntryDrawerComponent, TakaPipe],
})
export class InvoiceDetailsComponent implements OnInit, OnDestroy {

  invoiceId = 0;
  invoice:  SalesInvoiceDto | null = null;
  items:    SalesInvoiceItemDto[]    = [];
  payments: SalesInvoicePaymentDto[] = [];
  siteSettings: AdminSiteSettingDto | null = null;
  loading = true;

  isDrawerOpen = false;

  readonly shortStatus    = SalesInvoiceShortStatus;
  readonly describeSerials = describeSerials;

  private autoPrint = false;
  private originalTitle = document.title;

  constructor(
    private route:          ActivatedRoute,
    private router:         Router,
    private invoiceService: SalesInvoiceService,
    private siteSettingSvc: AdminSiteSettingService,
    private message:        NzMessageService,
  ) {}

  // ── Derived ────────────────────────────────────────────────────────────────

  get companyName(): string { return this.siteSettings?.siteName || 'DymoEnergy'; }

  get companyAddressLine(): string {
    const s = this.siteSettings;
    if (!s) return '';
    return [s.address, s.city, s.country].filter(Boolean).join(', ');
  }

  get companyContactLine(): string {
    const s = this.siteSettings;
    return [s?.phone, s?.email].filter(Boolean).join(' · ');
  }

  /** Number customers pay bKash / Nagad to — WhatsApp line if set, else main phone. */
  get payToNumber(): string {
    return this.siteSettings?.whatsApp || this.siteSettings?.phone || '';
  }

  /** "bKash ৳1,50,000 · Cash ৳90,000" — grouped by method, oldest first. */
  get paidBreakdown(): string {
    if (this.payments.length === 0) return '';
    const byMethod = new Map<string, number>();
    [...this.payments].reverse().forEach(p => {
      const label = SalesInvoicePaymentMethodLabels[p.method];
      byMethod.set(label, (byMethod.get(label) ?? 0) + p.amount);
    });
    return [...byMethod.entries()].map(([m, amt]) => `${m} ${formatTaka(amt)}`).join(' · ');
  }

  get isVoid(): boolean { return this.invoice?.status === 6 || this.invoice?.status === 7; }

  itemSubline(it: SalesInvoiceItemDto): string {
    return [describeSerials(it.serialNumbers), it.warranty, it.description].filter(Boolean).join(' · ');
  }

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.invoiceId = +this.route.snapshot.paramMap.get('id')!;
    if (!this.invoiceId) { this.router.navigate(['/sales-invoices']); return; }

    const q = this.route.snapshot.queryParamMap;
    this.autoPrint = q.has('print') || q.has('pdf');
    this.loadData();
  }

  ngOnDestroy(): void { document.title = this.originalTitle; }

  loadData(): void {
    this.loading = true;
    forkJoin({
      invoice:      this.invoiceService.get(this.invoiceId),
      items:        this.invoiceService.getItems(this.invoiceId),
      payments:     this.invoiceService.getPayments(this.invoiceId).pipe(catchError(() => of([] as SalesInvoicePaymentDto[]))),
      siteSettings: this.siteSettingSvc.getActive().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ invoice, items, payments, siteSettings }) => {
        this.invoice      = invoice;
        this.items        = items;
        this.payments     = payments;
        this.siteSettings = siteSettings;
        this.loading      = false;
        document.title    = invoice.invoiceNumber ?? 'Invoice';

        if (this.autoPrint) {
          this.autoPrint = false;
          // Let the paper render (fonts, logo) before opening the dialog
          setTimeout(() => this.print(), 400);
        }
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
}

import { Component, EventEmitter, HostListener, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { NzDropDownModule } from 'ng-zorro-antd/dropdown';
import { NzMenuModule } from 'ng-zorro-antd/menu';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import {
  SalesInvoiceDto,
  SalesInvoiceItemDto,
  SalesInvoicePaymentDto,
  SalesInvoicePaymentMethodLabels,
  SalesInvoicePaymentMethodOrder,
  SalesInvoicePaymentMethodType,
  SalesInvoiceShortStatus,
} from '../../../proxy/sales-invoices/models';
import { TakaPipe, formatTaka, initials, itemKind, parseSerials } from '../invoice-format';

type QvTab = 'items' | 'payments' | 'activity';

interface ActivityEntry {
  at:    string;
  title: string;
  sub?:  string;
  tone:  'green' | 'amber' | 'red' | 'gray' | 'blue';
  icon:  string;
}

@Component({
  selector:    'invoice-quick-view',
  templateUrl: './invoice-quick-view.component.html',
  styleUrl:    './invoice-quick-view.component.css',
  imports:     [SharedModule, NzModalModule, NzDropDownModule, NzMenuModule, TakaPipe],
})
export class InvoiceQuickViewComponent implements OnChanges {

  @Input() invoiceId: number | null = null;
  /** 1-based position of this invoice in the current page, for the "1 of 10" pager. */
  @Input() position = 0;
  @Input() total    = 0;

  @Output() prev    = new EventEmitter<void>();
  @Output() next    = new EventEmitter<void>();
  @Output() closed  = new EventEmitter<void>();
  /** Emitted after anything that changes list-visible data (payment, delete…). */
  @Output() changed = new EventEmitter<SalesInvoiceDto | null>();
  @Output() edit    = new EventEmitter<{ invoice: SalesInvoiceDto; items: SalesInvoiceItemDto[] }>();

  invoice:  SalesInvoiceDto | null = null;
  items:    SalesInvoiceItemDto[]    = [];
  payments: SalesInvoicePaymentDto[] = [];
  loading  = false;
  tab: QvTab = 'items';

  // ── Collect payment modal ────────────────────────────────────────────────
  collectOpen   = false;
  collecting    = false;
  collectAmount = 0;
  collectMethod: SalesInvoicePaymentMethodType = 1;
  collectDate: Date = new Date();
  collectRef    = '';
  collectNote   = '';

  deleting = false;
  deletingPaymentId: number | null = null;

  readonly methodLabels = SalesInvoicePaymentMethodLabels;
  readonly methodOrder  = SalesInvoicePaymentMethodOrder;
  readonly shortStatus  = SalesInvoiceShortStatus;
  readonly initials     = initials;
  readonly itemKind     = itemKind;

  constructor(
    private invoiceSvc: SalesInvoiceService,
    private message:    NzMessageService,
    private router:     Router,
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['invoiceId'] && this.invoiceId) this.load(this.invoiceId);
  }

  // Keyboard: ↑/↓ (or k/j) walk the list, Esc closes — ignored while typing or in the modal
  @HostListener('document:keydown', ['$event'])
  onKey(e: KeyboardEvent): void {
    const t = e.target as HTMLElement;
    if (this.collectOpen || /INPUT|TEXTAREA|SELECT/.test(t?.tagName) || t?.isContentEditable) return;
    if (e.key === 'ArrowUp'   || e.key === 'k') { e.preventDefault(); this.prev.emit(); }
    if (e.key === 'ArrowDown' || e.key === 'j') { e.preventDefault(); this.next.emit(); }
  }

  load(id: number): void {
    this.loading = true;
    forkJoin({
      invoice:  this.invoiceSvc.get(id),
      items:    this.invoiceSvc.getItems(id).pipe(catchError(() => of([] as SalesInvoiceItemDto[]))),
      payments: this.invoiceSvc.getPayments(id).pipe(catchError(() => of([] as SalesInvoicePaymentDto[]))),
    }).subscribe({
      next: ({ invoice, items, payments }) => {
        // Ignore stale responses when the user pages quickly
        if (id !== this.invoiceId) return;
        this.invoice  = invoice;
        this.items    = items;
        this.payments = payments;
        this.loading  = false;
      },
      error: () => {
        this.loading = false;
        this.message.error('Could not load invoice.');
      },
    });
  }

  // ── Derived view state ───────────────────────────────────────────────────

  get tone(): 'green' | 'amber' | 'red' | 'gray' | 'purple' {
    return this.invoice ? this.shortStatus[this.invoice.status].tone : 'gray';
  }

  get paidPct(): number {
    const inv = this.invoice;
    if (!inv || inv.grandTotal <= 0) return inv?.status === 3 ? 100 : 0;
    return Math.min(100, Math.round((inv.amountPaid / inv.grandTotal) * 100));
  }

  /** SVG ring: circumference of r=26 circle. */
  readonly ringC = 2 * Math.PI * 26;
  get ringOffset(): number { return this.ringC * (1 - this.paidPct / 100); }

  get canCollect(): boolean {
    const inv = this.invoice;
    return !!inv && inv.balanceDue > 0.005 && inv.status !== 6 && inv.status !== 7;
  }

  get daysOverdue(): number {
    if (!this.invoice?.dueDate) return 0;
    const ms = Date.now() - new Date(this.invoice.dueDate).getTime();
    return Math.max(0, Math.floor(ms / 86_400_000));
  }

  serialCount(item: SalesInvoiceItemDto): number { return parseSerials(item.serialNumbers).length; }

  get activity(): ActivityEntry[] {
    const inv = this.invoice;
    if (!inv) return [];
    const out: ActivityEntry[] = [];

    out.push({
      at: inv.creationTime ?? inv.invoiceDate, tone: 'gray', icon: 'file-add',
      title: inv.status === 1 ? 'Draft created' : 'Invoice issued',
      sub: `${formatTaka(inv.grandTotal)} · ${inv.itemCount} item${inv.itemCount === 1 ? '' : 's'}${inv.channel ? ' · ' + inv.channel : ''}`,
    });

    for (const p of [...this.payments].reverse()) {
      out.push({
        at: p.paidOn, tone: 'green', icon: 'wallet',
        title: `${formatTaka(p.amount)} received via ${this.methodLabels[p.method]}`,
        sub: [p.referenceNumber && `Ref ${p.referenceNumber}`, p.note].filter(Boolean).join(' · ') || undefined,
      });
    }

    if (inv.lastModificationTime && inv.creationTime &&
        new Date(inv.lastModificationTime).getTime() - new Date(inv.creationTime).getTime() > 60_000) {
      out.push({ at: inv.lastModificationTime, tone: 'blue', icon: 'edit', title: 'Invoice updated' });
    }

    if (inv.status === 5 && inv.dueDate) {
      out.push({ at: inv.dueDate, tone: 'red', icon: 'clock-circle', title: 'Became overdue', sub: `Due ${formatTaka(inv.balanceDue)}` });
    }
    if (inv.status === 6) out.push({ at: inv.lastModificationTime ?? inv.invoiceDate, tone: 'gray', icon: 'stop', title: 'Cancelled' });
    if (inv.status === 7) out.push({ at: inv.lastModificationTime ?? inv.invoiceDate, tone: 'gray', icon: 'rollback', title: 'Refunded' });

    return out.sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());
  }

  // ── Actions ──────────────────────────────────────────────────────────────

  openCollect(): void {
    if (!this.invoice) return;
    this.collectAmount = this.invoice.balanceDue;
    this.collectMethod = 1;
    this.collectDate   = new Date();
    this.collectRef    = '';
    this.collectNote   = '';
    this.collectOpen   = true;
  }

  setCollectFraction(f: number): void {
    if (this.invoice) this.collectAmount = Math.round(this.invoice.balanceDue * f);
  }

  submitCollect(): void {
    const inv = this.invoice;
    if (!inv) return;
    if (!(this.collectAmount > 0)) { this.message.warning('Enter an amount to collect.'); return; }
    if (this.collectAmount > inv.balanceDue + 0.005) {
      this.message.warning(`Amount is more than the ${formatTaka(inv.balanceDue)} still due.`);
      return;
    }

    this.collecting = true;
    this.invoiceSvc.collectPayment(inv.id, {
      amount:          this.collectAmount,
      method:          this.collectMethod,
      paidOn:          this.collectDate?.toISOString(),
      referenceNumber: this.collectRef || undefined,
      note:            this.collectNote || undefined,
    }).subscribe({
      next: updated => {
        this.collecting  = false;
        this.collectOpen = false;
        this.message.success(`${formatTaka(this.collectAmount)} collected.`);
        this.invoice = updated;
        this.tab     = 'payments';
        this.invoiceSvc.getPayments(inv.id).subscribe(p => this.payments = p);
        this.changed.emit(updated);
      },
      error: () => { this.collecting = false; },
    });
  }

  deletePayment(p: SalesInvoicePaymentDto): void {
    if (!this.invoice) return;
    this.deletingPaymentId = p.id;
    this.invoiceSvc.deletePayment(this.invoice.id, p.id).subscribe({
      next: updated => {
        this.deletingPaymentId = null;
        this.invoice  = updated;
        this.payments = this.payments.filter(x => x.id !== p.id);
        this.message.success('Payment removed.');
        this.changed.emit(updated);
      },
      error: () => { this.deletingPaymentId = null; },
    });
  }

  deleteInvoice(): void {
    if (!this.invoice) return;
    this.deleting = true;
    this.invoiceSvc.delete(this.invoice.id).subscribe({
      next: () => {
        this.deleting = false;
        this.message.success('Invoice deleted.');
        this.changed.emit(null);
        this.closed.emit();
      },
      error: () => { this.deleting = false; },
    });
  }

  openEdit(): void {
    if (this.invoice) this.edit.emit({ invoice: this.invoice, items: this.items });
  }

  /** Opens the printable invoice in a new tab; mode 'pdf' names the tab for "Save as PDF". */
  openPrintable(mode: 'print' | 'pdf'): void {
    if (!this.invoice) return;
    window.open(`/sales-invoices/${this.invoice.id}?${mode}=1`, '_blank', 'noopener');
  }

  openFullPage(): void {
    if (this.invoice) window.open(`/sales-invoices/${this.invoice.id}`, '_blank', 'noopener');
  }

  private get reminderText(): string {
    const inv = this.invoice!;
    const name = (inv.customerName ?? '').replace(/[\[\]]/g, '') || 'Customer';
    const lines = [
      `Dear ${name}, thank you for choosing DymoEnergy.`,
      `Invoice ${inv.invoiceNumber}: total ${formatTaka(inv.grandTotal)}, paid ${formatTaka(inv.amountPaid)}.`,
    ];
    if (inv.balanceDue > 0.005) {
      lines.push(`Amount due: ${formatTaka(inv.balanceDue)}. Pay by bKash / Nagad with reference ${inv.invoiceNumber}, or at the showroom.`);
    }
    return lines.join('\n');
  }

  private get phoneDigits(): string {
    let d = (this.invoice?.customerPhone ?? '').replace(/\D/g, '');
    // Local BD mobile (01XXXXXXXXX) → international 8801XXXXXXXXX
    if (d.length === 11 && d.startsWith('0')) d = '88' + d;
    return d;
  }

  sendSms(): void {
    if (!this.invoice?.customerPhone) { this.message.warning('No phone number on this invoice.'); return; }
    window.location.href = `sms:+${this.phoneDigits}?body=${encodeURIComponent(this.reminderText)}`;
  }

  sendWhatsApp(): void {
    if (!this.invoice?.customerPhone) { this.message.warning('No phone number on this invoice.'); return; }
    window.open(`https://wa.me/${this.phoneDigits}?text=${encodeURIComponent(this.reminderText)}`, '_blank', 'noopener');
  }

  startReturn(): void {
    this.message.info('Product returns are not enabled yet.');
  }

  openProfile(): void {
    const inv = this.invoice;
    if (!inv) return;
    this.router.navigate(['/customers'], {
      queryParams: inv.customerId ? { id: inv.customerId } : { search: inv.customerPhone || inv.customerName },
    });
  }

  close(): void { this.closed.emit(); }
}

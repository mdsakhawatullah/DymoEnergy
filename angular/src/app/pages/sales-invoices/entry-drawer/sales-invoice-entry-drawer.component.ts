import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject, Subscription, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { SharedModule } from '../../../shared/shared.module';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import { ProductService } from '../../../proxy/products/product.service';
import { ProductDto } from '../../../proxy/products/models';
import {
  CreateUpdateSalesInvoiceDto,
  SalesInvoiceDto,
  SalesInvoiceItemDto,
  SalesInvoicePaymentMethodLabels,
  SalesInvoicePaymentMethodOrder,
} from '../../../proxy/sales-invoices/models';
import { TakaPipe, parseSerials } from '../invoice-format';

/** Statuses the user can pick; Paid / Partially paid / Overdue are derived by the server. */
const EDITABLE_STATUSES = [
  { value: 2, label: 'Issued' },
  { value: 1, label: 'Draft' },
  { value: 6, label: 'Cancelled' },
  { value: 7, label: 'Refunded' },
];

@Component({
  selector:    'sales-invoice-entry-drawer',
  templateUrl: './sales-invoice-entry-drawer.component.html',
  styleUrl:    './sales-invoice-entry-drawer.component.css',
  imports:     [SharedModule, TakaPipe],
})
export class SalesInvoiceEntryDrawerComponent implements OnInit, OnChanges, OnDestroy {

  @Input()  input: SalesInvoiceDto | null = null;
  @Input()  existingItems: SalesInvoiceItemDto[] = [];

  @Output() onDrawerClosed     = new EventEmitter<void>();
  @Output() handleInvoiceSaved = new EventEmitter<SalesInvoiceDto>();

  form!: FormGroup;
  saving = false;

  // ── Computed totals (preview only — the server recalculates on save) ──────
  subtotal      = 0;
  lineDiscounts = 0;
  taxTotal      = 0;
  grandTotal    = 0;
  balanceDue    = 0;

  readonly statusOptions  = EDITABLE_STATUSES;
  readonly methodLabels   = SalesInvoicePaymentMethodLabels;
  readonly methodOrder    = SalesInvoicePaymentMethodOrder;
  readonly channelOptions = ['Showroom POS', 'Online store', 'Field sale', 'Phone order', 'Dealer'];

  // ── Product picker ────────────────────────────────────────────────────────
  productResults: ProductDto[] = [];
  productSearching = false;
  pickedProductId: number | null = null;
  private productSearch$ = new Subject<string>();
  private subs = new Subscription();

  constructor(
    private fb:         FormBuilder,
    private invoiceSvc: SalesInvoiceService,
    private productSvc: ProductService,
    private message:    NzMessageService,
  ) {
    this.buildForm();
  }

  get isEdit(): boolean { return !!this.input?.id; }

  ngOnInit(): void {
    this.subs.add(
      this.productSearch$.pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(q => {
          this.productSearching = true;
          return this.productSvc.getListData({ filter: q || undefined, maxResultCount: 20, skipCount: 0 })
            .pipe(catchError(() => of({ items: [], totalCount: 0 })));
        }),
      ).subscribe(r => {
        this.productResults   = r.items;
        this.productSearching = false;
      }),
    );
    this.productSearch$.next('');

    this.subs.add(this.form.get('invoiceDate')!.valueChanges.subscribe(() => this.defaultDueDate()));
    // Any edit (qty, price, VAT toggle, discount…) refreshes the preview totals.
    // recalcTotals writes lineTotal with emitEvent:false, so this cannot loop.
    this.subs.add(this.form.valueChanges.subscribe(() => this.recalcTotals()));
  }

  ngOnDestroy(): void { this.subs.unsubscribe(); }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        this.form.patchValue({
          invoiceDate:        this.input.invoiceDate ? new Date(this.input.invoiceDate) : new Date(),
          dueDate:            this.input.dueDate ? new Date(this.input.dueDate) : null,
          customerName:       this.input.customerName,
          customerEmail:      this.input.customerEmail,
          customerPhone:      this.input.customerPhone,
          billingAddress:     this.input.billingAddress,
          shippingAddress:    this.input.shippingAddress,
          shippingCost:       this.input.shippingCost,
          additionalDiscount: this.input.additionalDiscount ?? 0,
          discountNote:       this.input.discountNote,
          taxInclusive:       this.input.taxInclusive ?? false,
          referenceNumber:    this.input.referenceNumber,
          currencyCode:       this.input.currencyCode,
          channel:            this.input.channel,
          // Derived statuses (paid / partially paid / overdue) show as Issued
          status:             [1, 6, 7].includes(this.input.status) ? this.input.status : 2,
          amountPaid:         this.input.amountPaid,
          notes:              this.input.notes,
          terms:              this.input.terms,
        }, { emitEvent: false });
      } else {
        const today = new Date();
        this.form.reset({
          status: 2, currencyCode: 'BDT', channel: 'Showroom POS',
          invoiceDate: today, dueDate: this.addDays(today, 30),
          amountPaid: 0, paymentMethod: 1, shippingCost: 0,
          additionalDiscount: 0, taxInclusive: true,
        }, { emitEvent: false });
        this.items.clear();
      }
      this.recalcTotals();
    }

    if (changes['existingItems'] && this.existingItems?.length) {
      this.items.clear();
      this.existingItems.forEach(item => this.items.push(this.buildItemGroup(item)));
      this.recalcTotals();
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      invoiceDate:        [new Date()],
      dueDate:            [null],
      customerName:       [null],
      customerEmail:      [null],
      customerPhone:      [null],
      billingAddress:     [null],
      shippingAddress:    [null],
      shippingCost:       [0],
      additionalDiscount: [0],
      discountNote:       [null],
      taxInclusive:       [true],
      referenceNumber:    [null],
      currencyCode:       ['BDT'],
      channel:            ['Showroom POS'],
      status:             [2],
      paymentMethod:      [1],
      amountPaid:         [0],
      notes:              [null],
      terms:              [null],
      items:              this.fb.array([]),
    });
  }

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  buildItemGroup(item?: Partial<SalesInvoiceItemDto>): FormGroup {
    return this.fb.group({
      id:              [item?.id ?? null],
      productId:       [item?.productId ?? null],
      productName:     [item?.productName ?? null],
      sku:             [item?.sku ?? null],
      description:     [item?.description ?? null],
      quantity:        [item?.quantity ?? 1],
      unitPrice:       [item?.unitPrice ?? 0],
      discountPercent: [item?.discountPercent ?? 0],
      taxRate:         [item?.taxRate ?? 5],
      lineTotal:       [item?.lineTotal ?? 0],
      serialNumbers:   [item?.serialNumbers ?? null],
      warranty:        [item?.warranty ?? null],
      showExtra:       [!!(item?.serialNumbers || item?.warranty || item?.description)],
    });
  }

  // ── Line items ────────────────────────────────────────────────────────────

  onProductSearch(q: string): void { this.productSearch$.next(q); }

  onProductPicked(id: number | null): void {
    const p = this.productResults.find(x => x.id === id);
    if (!p) return;
    const price = p.discountPrice && p.discountPrice > 0 ? p.discountPrice : p.price;
    this.items.push(this.buildItemGroup({
      productId: p.id, productName: p.name, sku: p.sku, unitPrice: price, quantity: 1,
    }));
    this.recalcTotals();
    // Reset the picker so the same product can be added again
    setTimeout(() => this.pickedProductId = null);
  }

  addItem(): void {
    this.items.push(this.buildItemGroup());
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.recalcTotals();
  }

  toggleExtra(index: number): void {
    const c = this.items.at(index).get('showExtra')!;
    c.setValue(!c.value);
  }

  serialCount(index: number): number {
    return parseSerials(this.items.at(index).get('serialNumbers')?.value).length;
  }

  /** Mirrors SalesInvoiceAppService.RecalculateTotals so the preview matches what gets saved. */
  recalcTotals(): void {
    const inclusive  = !!this.form.get('taxInclusive')?.value;
    let subtotal = 0, lineDisc = 0, tax = 0, net = 0, lines = 0;

    this.items.controls.forEach(ctrl => {
      const row   = ctrl.value;
      const gross = (+row.quantity || 0) * (+row.unitPrice || 0);
      const disc  = gross * Math.min(100, Math.max(0, +row.discountPercent || 0)) / 100;
      const n     = gross - disc;
      const rate  = +row.taxRate || 0;
      const t     = inclusive ? (rate > 0 ? n - n / (1 + rate / 100) : 0) : n * rate / 100;
      const line  = inclusive ? n : n + t;

      ctrl.get('lineTotal')!.setValue(+line.toFixed(2), { emitEvent: false });
      subtotal += gross; lineDisc += disc; tax += t; net += n; lines += line;
    });

    const additional = Math.max(0, +this.form.get('additionalDiscount')?.value || 0);
    if (inclusive && additional > 0 && net > 0) tax *= Math.max(0, net - additional) / net;

    const shipping = Math.max(0, +this.form.get('shippingCost')?.value || 0);
    const grand    = Math.max(0, lines - additional + shipping);
    const paid     = +this.form.get('amountPaid')?.value || 0;

    this.subtotal      = subtotal;
    this.lineDiscounts = lineDisc;
    this.taxTotal      = tax;
    this.grandTotal    = grand;
    this.balanceDue    = Math.max(0, grand - paid);
  }

  setPaidFull(): void {
    this.form.patchValue({ amountPaid: Math.round(this.grandTotal) });
    this.recalcTotals();
  }

  private addDays(d: Date, days: number): Date {
    const r = new Date(d);
    r.setDate(r.getDate() + days);
    return r;
  }

  /** New invoices: keep due date 30 days after the invoice date until the user changes it. */
  private defaultDueDate(): void {
    if (this.isEdit) return;
    const due = this.form.get('dueDate');
    if (due && !due.dirty) due.setValue(this.addDays(this.form.get('invoiceDate')!.value ?? new Date(), 30), { emitEvent: false });
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  save(): void {
    const v = this.form.value;

    if (this.items.length === 0) { this.message.warning('Add at least one item.'); return; }
    if ((v.items as any[]).some(r => !r.productName?.trim())) { this.message.warning('Every item needs a name.'); return; }
    if (!this.isEdit && (+v.amountPaid || 0) > this.grandTotal + 0.005) {
      this.message.warning('Amount received is more than the invoice total.');
      return;
    }

    const iso = (d: unknown) => d instanceof Date ? d.toISOString() : (d as string) ?? undefined;
    const payload: CreateUpdateSalesInvoiceDto = {
      invoiceDate:        iso(v.invoiceDate)!,
      dueDate:            iso(v.dueDate),
      customerId:         this.input?.customerId,
      customerName:       v.customerName,
      customerEmail:      v.customerEmail,
      customerPhone:      v.customerPhone,
      billingAddress:     v.billingAddress,
      shippingAddress:    v.shippingAddress,
      shippingCost:       +v.shippingCost || 0,
      additionalDiscount: +v.additionalDiscount || 0,
      discountNote:       v.discountNote,
      taxInclusive:       !!v.taxInclusive,
      referenceNumber:    v.referenceNumber,
      currencyCode:       v.currencyCode || 'BDT',
      channel:            v.channel,
      status:             v.status,
      amountPaid:         this.isEdit ? 0 : (+v.amountPaid || 0),
      paymentMethod:      this.isEdit ? undefined : v.paymentMethod,
      paymentDate:        this.isEdit ? undefined : iso(v.invoiceDate),
      notes:              v.notes,
      terms:              v.terms,
      items: (v.items as any[]).map((row, idx) => ({
        id:              row.id ?? undefined,
        productId:       row.productId ?? undefined,
        productName:     row.productName?.trim(),
        sku:             row.sku,
        description:     row.description,
        quantity:        +row.quantity || 0,
        unitPrice:       +row.unitPrice || 0,
        discountPercent: +row.discountPercent || 0,
        discountAmount:  0,
        taxRate:         +row.taxRate || 0,
        taxAmount:       0,
        lineTotal:       row.lineTotal ?? 0,
        serialNumbers:   parseSerials(row.serialNumbers).join(', ') || undefined,
        warranty:        row.warranty || undefined,
        displayOrder:    idx,
      })),
    };

    this.saving = true;
    const req$ = this.isEdit
      ? this.invoiceSvc.update(this.input!.id, payload)
      : this.invoiceSvc.create(payload);

    req$.subscribe({
      next: saved => {
        this.message.success(this.isEdit ? 'Invoice updated.' : `Invoice ${saved.invoiceNumber} created.`);
        this.saving = false;
        this.handleInvoiceSaved.emit(saved);
      },
      error: () => { this.saving = false; },
    });
  }

  close(): void { this.onDrawerClosed.emit(); }
}

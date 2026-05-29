import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import {
  SalesInvoiceDto,
  SalesInvoiceItemDto,
  SalesInvoiceStatusLabels,
  SalesInvoicePaymentMethodLabels,
} from '../../../proxy/sales-invoices/models';

@Component({
  selector:    'sales-invoice-entry-drawer',
  templateUrl: './sales-invoice-entry-drawer.component.html',
  styleUrl:    './sales-invoice-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class SalesInvoiceEntryDrawerComponent implements OnChanges {

  @Input()  input: SalesInvoiceDto | null = null;
  @Input()  existingItems: SalesInvoiceItemDto[] = [];

  @Output() onDrawerClosed     = new EventEmitter<void>();
  @Output() handleInvoiceSaved = new EventEmitter<void>();

  form!: FormGroup;
  saving = false;

  // ── Computed totals (plain properties — not form controls) ────────────────
  subtotal      = 0;
  discountTotal = 0;
  taxTotal      = 0;
  shippingCost  = 0;
  grandTotal    = 0;
  balanceDue    = 0;

  statusOptions = Object.entries(SalesInvoiceStatusLabels).map(([value, label]) => ({
    value: +value, label,
  }));

  paymentMethodOptions = Object.entries(SalesInvoicePaymentMethodLabels).map(([value, label]) => ({
    value: +value, label,
  }));

  constructor(
    private fb:         FormBuilder,
    private invoiceSvc: SalesInvoiceService,
    private message:    NzMessageService,
  ) {
    this.buildForm();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        this.form.patchValue({
          invoiceDate:     this.input.invoiceDate ? new Date(this.input.invoiceDate) : new Date(),
          dueDate:         this.input.dueDate ? new Date(this.input.dueDate) : null,
          customerName:    this.input.customerName,
          customerEmail:   this.input.customerEmail,
          customerPhone:   this.input.customerPhone,
          billingAddress:  this.input.billingAddress,
          shippingAddress: this.input.shippingAddress,
          shippingCost:    this.input.shippingCost,
          referenceNumber: this.input.referenceNumber,
          currencyCode:    this.input.currencyCode,
          status:          this.input.status,
          paymentMethod:   this.input.paymentMethod,
          paymentDate:     this.input.paymentDate ? new Date(this.input.paymentDate) : null,
          amountPaid:      this.input.amountPaid,
          notes:           this.input.notes,
          terms:           this.input.terms,
        });
        // Seed totals from existing record
        this.subtotal      = this.input.subtotal;
        this.discountTotal = this.input.discountTotal;
        this.taxTotal      = this.input.taxTotal;
        this.shippingCost  = this.input.shippingCost;
        this.grandTotal    = this.input.grandTotal;
        this.balanceDue    = this.input.balanceDue;
      } else {
        this.form.reset({
          status: 1, currencyCode: 'BDT',
          invoiceDate: new Date(), amountPaid: 0,
        });

        this.items.clear();
        this.subtotal = this.discountTotal = this.taxTotal = this.shippingCost = this.grandTotal = this.balanceDue = 0;
      }
    }

    if (changes['existingItems'] && this.existingItems?.length) {
      this.items.clear();
      this.existingItems.forEach(item => this.items.push(this.buildItemGroup(item)));
      this.recalcTotals();
    }
  }

  buildForm(): void {
    this.form = this.fb.group({
      invoiceDate:     [new Date()],
      dueDate:         [null],
      customerName:    [null],
      customerEmail:   [null],
      customerPhone:   [null],
      billingAddress:  [null],
      shippingAddress: [null],
      shippingCost:    [0],
      referenceNumber: [null],
      currencyCode:    ['BDT'],
      status:          [1],
      paymentMethod:   [null],
      paymentDate:     [null],
      amountPaid:      [0],
      notes:           [null],
      terms:           [null],
      items:           this.fb.array([]),
    });
  }

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  buildItemGroup(item?: Partial<SalesInvoiceItemDto>): FormGroup {
    return this.fb.group({
      id:              [item?.id ?? null],
      productName:     [item?.productName ?? null],
      sku:             [item?.sku ?? null],
      description:     [item?.description ?? null],
      quantity:        [item?.quantity ?? 1],
      unitPrice:       [item?.unitPrice ?? 0],
      discountPercent: [item?.discountPercent ?? 0],
      discountAmount:  [item?.discountAmount ?? 0],
      taxRate:         [item?.taxRate ?? 0],
      taxAmount:       [item?.taxAmount ?? 0],
      lineTotal:       [item?.lineTotal ?? 0],
      displayOrder:    [item?.displayOrder ?? 0],
    });
  }

  addItem(): void {
    this.items.push(this.buildItemGroup());
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.recalcTotals();
  }

  recalcRow(index: number): void {
    const row  = this.items.at(index) as FormGroup;
    const qty  = +row.get('quantity')?.value  || 0;
    const up   = +row.get('unitPrice')?.value || 0;
    const disc = +row.get('discountPercent')?.value || 0;
    const tax  = +row.get('taxRate')?.value   || 0;

    const gross    = qty * up;
    const discAmt  = gross * disc / 100;
    const taxAmt   = (gross - discAmt) * tax / 100;
    const total    = gross - discAmt + taxAmt;

    row.patchValue({
      discountAmount: +discAmt.toFixed(2),
      taxAmount:      +taxAmt.toFixed(2),
      lineTotal:      +total.toFixed(2),
    }, { emitEvent: false });

    this.recalcTotals();
  }

  recalcTotals(): void {
    let subtotal = 0, discTotal = 0, taxTotal = 0;

    (this.items.value as any[]).forEach(row => {
      const qty   = +row.quantity  || 0;
      const up    = +row.unitPrice || 0;
      const disc  = +row.discountAmount || 0;
      const tax   = +row.taxAmount || 0;

      subtotal  += qty * up;
      discTotal += disc;
      taxTotal  += tax;
    });

    const shipping = +this.form.get('shippingCost')?.value || 0;
    const grand    = subtotal - discTotal + taxTotal + shipping;
    const amtPaid  = +this.form.get('amountPaid')?.value || 0;

    this.subtotal      = +subtotal.toFixed(2);
    this.discountTotal = +discTotal.toFixed(2);
    this.taxTotal      = +taxTotal.toFixed(2);
    this.shippingCost  = +shipping.toFixed(2);
    this.grandTotal    = +grand.toFixed(2);
    this.balanceDue    = +(grand - amtPaid).toFixed(2);
  }

  onAmountPaidChange(): void { this.recalcTotals(); }
  onShippingCostChange(): void { this.recalcTotals(); }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;

    const v = this.form.value;
    const payload = {
      invoiceDate:     v.invoiceDate instanceof Date ? v.invoiceDate.toISOString() : v.invoiceDate,
      dueDate:         v.dueDate instanceof Date ? v.dueDate.toISOString() : v.dueDate ?? undefined,
      customerName:    v.customerName,
      customerEmail:   v.customerEmail,
      customerPhone:   v.customerPhone,
      billingAddress:  v.billingAddress,
      shippingAddress: v.shippingAddress,
      shippingCost:    v.shippingCost ?? 0,
      referenceNumber: v.referenceNumber,
      currencyCode:    v.currencyCode ?? 'BDT',
      status:          v.status,
      paymentMethod:   v.paymentMethod ?? undefined,
      paymentDate:     v.paymentDate instanceof Date ? v.paymentDate.toISOString() : v.paymentDate ?? undefined,
      amountPaid:      v.amountPaid ?? 0,
      notes:           v.notes,
      terms:           v.terms,
      subtotal:        this.subtotal,
      discountTotal:   this.discountTotal,
      taxTotal:        this.taxTotal,
      grandTotal:      this.grandTotal,
      balanceDue:      this.balanceDue,
      items: (v.items as any[]).map((row, idx) => ({
        id:              row.id ?? undefined,
        productName:     row.productName,
        sku:             row.sku,
        description:     row.description,
        quantity:        row.quantity     ?? 1,
        unitPrice:       row.unitPrice    ?? 0,
        discountPercent: row.discountPercent ?? 0,
        discountAmount:  row.discountAmount  ?? 0,
        taxRate:         row.taxRate      ?? 0,
        taxAmount:       row.taxAmount    ?? 0,
        lineTotal:       row.lineTotal    ?? 0,
        displayOrder:    idx,
      })),
    };

    const req$ = this.input?.id
      ? this.invoiceSvc.update(this.input.id, payload as any)
      : this.invoiceSvc.create(payload as any);

    req$.subscribe({
      next: () => {
        this.message.success(this.input?.id ? 'Invoice updated.' : 'Invoice created.');
        this.saving = false;
        this.handleInvoiceSaved.emit();
      },
      error: () => {
        this.message.error('Something went wrong. Please try again.');
        this.saving = false;
      },
    });
  }

  close(): void { this.onDrawerClosed.emit(); }
}

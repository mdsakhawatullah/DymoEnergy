import {
  Component, EventEmitter, Input,
  OnChanges, OnDestroy, OnInit, Output, SimpleChanges,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { OrderService } from '../../../proxy/orders/order.service';
import {
  OrderDto,
  OrderItemDto,
  OrderStatusLabels,
  OrderStageLabels,
  OrderPriorityLabels,
  OrderShipmentTypeLabels,
  OrderPaymentTypeLabels,
  OrderCreateMethodLabels,
} from '../../../proxy/orders/models';

@Component({
  selector:    'order-entry-drawer',
  templateUrl: './order-entry-drawer.component.html',
  styleUrl:    './order-entry-drawer.component.css',
  imports:     [SharedModule],
})
export class OrderEntryDrawerComponent implements OnInit, OnChanges, OnDestroy {

  @Input()  input: OrderDto | null = null;
  @Input()  existingItems: OrderItemDto[] = [];

  @Output() onDrawerClosed   = new EventEmitter<void>();
  @Output() handleOrderSaved = new EventEmitter<void>();

  form!:   FormGroup;
  saving = false;

  // ── Computed totals ───────────────────────────────────────────────────────
  subtotal      = 0;
  discountTotal = 0;
  taxTotal      = 0;
  grandTotal    = 0;
  balanceDue    = 0;

  // ── Select options ────────────────────────────────────────────────────────
  statusOptions       = this.toOptions(OrderStatusLabels);
  stageOptions        = this.toOptions(OrderStageLabels);
  priorityOptions     = this.toOptions(OrderPriorityLabels);
  shipmentTypeOptions = this.toOptions(OrderShipmentTypeLabels);
  paymentTypeOptions  = this.toOptions(OrderPaymentTypeLabels);
  createMethodOptions = this.toOptions(OrderCreateMethodLabels);

  private destroy$ = new Subject<void>();

  constructor(
    private fb:       FormBuilder,
    private orderSvc: OrderService,
    private message:  NzMessageService,
  ) {
    this.buildForm();
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // React to shipping / voucher / amount-paid changes (non-item fields)
    ['shippingCost', 'voucherAmount', 'amountPaid'].forEach(field => {
      this.form.get(field)!.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(() => this.recalcTotals());
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) {
        // ── EDIT: patch header fields, keep items until existingItems arrives
        this.form.patchValue({
          orderDate:             this.input.orderDate             ? new Date(this.input.orderDate) : new Date(),
          estimatedDeliveryDate: this.input.estimatedDeliveryDate ? new Date(this.input.estimatedDeliveryDate) : null,
          actualDeliveryDate:    this.input.actualDeliveryDate    ? new Date(this.input.actualDeliveryDate)    : null,
          customerId:        this.input.customerId,
          customerName:      this.input.customerName,
          customerEmail:     this.input.customerEmail,
          customerPhone:     this.input.customerPhone,
          customerReference: this.input.customerReference,
          billingAddress:    this.input.billingAddress,
          deliveryAddress:   this.input.deliveryAddress,
          deliveryContact:   this.input.deliveryContact,
          deliveryPhone:     this.input.deliveryPhone,
          status:       this.input.status,
          stage:        this.input.stage,
          priority:     this.input.priority,
          shipmentType: this.input.shipmentType,
          paymentType:  this.input.paymentType,
          createMethod: this.input.createMethod,
          createdHow:   this.input.createdHow,
          voucherCode:   this.input.voucherCode,
          voucherAmount: this.input.voucherAmount,
          currencyCode:  this.input.currencyCode,
          taxRate:       this.input.taxRate,
          shippingCost:  this.input.shippingCost,
          paymentDate:   this.input.paymentDate ? new Date(this.input.paymentDate) : null,
          amountPaid:    this.input.amountPaid,
          notes:         this.input.notes,
          notesInvoice:  this.input.notesInvoice,
          terms:         this.input.terms,
          internalNotes: this.input.internalNotes,
        });
        // Restore display totals from saved values
        this.subtotal      = this.input.subtotal;
        this.discountTotal = this.input.discountTotal;
        this.taxTotal      = this.input.taxTotal;
        this.grandTotal    = this.input.grandTotal;
        this.balanceDue    = this.input.balanceDue;
      } else {
        // ── CREATE: reset everything
        this.form.reset({
          status: 1, stage: 1, priority: 2, shipmentType: 1, createMethod: 1,
          currencyCode: 'BDT', orderDate: new Date(),
          voucherAmount: 0, taxRate: 0, shippingCost: 0, amountPaid: 0,
        });
        this.clearItems();
        this.subtotal = this.discountTotal = this.taxTotal = this.grandTotal = this.balanceDue = 0;
      }
    }

    if (changes['existingItems'] && this.existingItems?.length) {
      this.clearItems();
      this.existingItems.forEach(item => this.pushItem(this.buildItemGroup(item)));
      this.recalcTotals();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Form ──────────────────────────────────────────────────────────────────

  buildForm(): void {
    this.form = this.fb.group({
      orderDate:             [new Date()],
      estimatedDeliveryDate: [null],
      actualDeliveryDate:    [null],
      customerId:            [null],
      customerName:          [null],
      customerEmail:         [null],
      customerPhone:         [null],
      customerReference:     [null],
      billingAddress:        [null],
      deliveryAddress:       [null],
      deliveryContact:       [null],
      deliveryPhone:         [null],
      status:       [1],
      stage:        [1],
      priority:     [2],
      shipmentType: [1],
      paymentType:  [null],
      createMethod: [1],
      createdHow:   [null],
      voucherCode:   [null],
      voucherAmount: [0],
      currencyCode:  ['BDT'],
      taxRate:       [0],
      shippingCost:  [0],
      paymentDate:   [null],
      amountPaid:    [0],
      notes:         [null],
      notesInvoice:  [null],
      terms:         [null],
      internalNotes: [null],
      items:         this.fb.array([]),
    });
  }

  get items(): FormArray { return this.form.get('items') as FormArray; }

  buildItemGroup(item?: Partial<OrderItemDto>): FormGroup {
    return this.fb.group({
      id:              [item?.id              ?? null],
      productId:       [item?.productId       ?? null],
      productName:     [item?.productName     ?? null],
      sku:             [item?.sku             ?? null],
      description:     [item?.description     ?? null],
      quantity:        [item?.quantity        ?? 1],
      unitPrice:       [item?.unitPrice       ?? 0],
      discountPercent: [item?.discountPercent ?? 0],
      discountAmount:  [item?.discountAmount  ?? 0],
      taxRate:         [item?.taxRate         ?? 0],
      taxAmount:       [item?.taxAmount       ?? 0],
      lineTotal:       [item?.lineTotal       ?? 0],
      displayOrder:    [item?.displayOrder    ?? 0],
    });
  }

  /**
   * Add an item group AND wire up valueChanges so the row recalculates
   * whenever quantity / unitPrice / discountPercent / taxRate change.
   */
  private pushItem(group: FormGroup): void {
    this.items.push(group);
    const idx = this.items.length - 1;

    ['quantity', 'unitPrice', 'discountPercent', 'taxRate'].forEach(field => {
      group.get(field)!.valueChanges
        .pipe(takeUntil(this.destroy$))
        .subscribe(() => {
          // Use setTimeout(0) so Angular's ControlValueAccessor finishes
          // writing the value before we read it back.
          setTimeout(() => this.recalcRow(idx));
        });
    });
  }

  addItem(): void {
    this.pushItem(this.buildItemGroup());
  }

  removeItem(index: number): void {
    this.items.removeAt(index);
    this.recalcTotals();
  }

  private clearItems(): void {
    this.items.clear();
  }

  // ── Recalculation ─────────────────────────────────────────────────────────

  recalcRow(index: number): void {
    const row  = this.items.at(index) as FormGroup;
    if (!row) return;

    const qty  = +(row.get('quantity')?.value       ?? 0) || 0;
    const up   = +(row.get('unitPrice')?.value      ?? 0) || 0;
    const disc = +(row.get('discountPercent')?.value ?? 0) || 0;
    const tax  = +(row.get('taxRate')?.value         ?? 0) || 0;

    const gross   = qty * up;
    const discAmt = +(gross * disc / 100).toFixed(2);
    const taxAmt  = +((gross - discAmt) * tax / 100).toFixed(2);
    const total   = +(gross - discAmt + taxAmt).toFixed(2);

    row.patchValue(
      { discountAmount: discAmt, taxAmount: taxAmt, lineTotal: total },
      { emitEvent: false },    // prevents valueChanges loop
    );

    this.recalcTotals();
  }

  recalcTotals(): void {
    let sub = 0, disc = 0, tax = 0;

    (this.items.value as any[]).forEach(r => {
      sub  += (+(r.quantity  || 0)) * (+(r.unitPrice || 0));
      disc += +(r.discountAmount || 0);
      tax  += +(r.taxAmount      || 0);
    });

    const shipping = +(this.form.get('shippingCost')?.value  || 0);
    const voucher  = +(this.form.get('voucherAmount')?.value || 0);
    const amtPaid  = +(this.form.get('amountPaid')?.value    || 0);
    const grand    = sub - disc + tax + shipping - voucher;

    this.subtotal      = +sub.toFixed(2);
    this.discountTotal = +disc.toFixed(2);
    this.taxTotal      = +tax.toFixed(2);
    this.grandTotal    = +grand.toFixed(2);
    this.balanceDue    = +(grand - amtPaid).toFixed(2);
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving = true;

    const v = this.form.value;
    const toIso = (d: any): string | undefined =>
      d instanceof Date ? d.toISOString() : (d ?? undefined);

    const payload = {
      orderDate:             toIso(v.orderDate) ?? new Date().toISOString(),
      estimatedDeliveryDate: toIso(v.estimatedDeliveryDate),
      actualDeliveryDate:    toIso(v.actualDeliveryDate),
      customerId:        v.customerId,
      customerName:      v.customerName,
      customerEmail:     v.customerEmail,
      customerPhone:     v.customerPhone,
      customerReference: v.customerReference,
      billingAddress:    v.billingAddress,
      deliveryAddress:   v.deliveryAddress,
      deliveryContact:   v.deliveryContact,
      deliveryPhone:     v.deliveryPhone,
      status:       v.status,
      stage:        v.stage,
      priority:     v.priority,
      shipmentType: v.shipmentType,
      paymentType:  v.paymentType ?? undefined,
      createMethod: v.createMethod,
      createdHow:   v.createdHow,
      voucherCode:   v.voucherCode,
      voucherAmount: v.voucherAmount  ?? 0,
      currencyCode:  v.currencyCode   ?? 'BDT',
      taxRate:       v.taxRate        ?? 0,
      shippingCost:  v.shippingCost   ?? 0,
      subtotal:      this.subtotal,
      discountTotal: this.discountTotal,
      taxTotal:      this.taxTotal,
      grandTotal:    this.grandTotal,
      amountPaid:    v.amountPaid     ?? 0,
      balanceDue:    this.balanceDue,
      paymentDate:   toIso(v.paymentDate),
      notes:         v.notes,
      notesInvoice:  v.notesInvoice,
      terms:         v.terms,
      internalNotes: v.internalNotes,
      items: (v.items as any[]).map((row, idx) => ({
        id:              row.id              ?? undefined,
        productId:       row.productId       ?? undefined,
        productName:     row.productName,
        sku:             row.sku,
        description:     row.description,
        quantity:        +(row.quantity        ?? 1),
        unitPrice:       +(row.unitPrice       ?? 0),
        discountPercent: +(row.discountPercent ?? 0),
        discountAmount:  +(row.discountAmount  ?? 0),
        taxRate:         +(row.taxRate         ?? 0),
        taxAmount:       +(row.taxAmount       ?? 0),
        lineTotal:       +(row.lineTotal       ?? 0),
        displayOrder:    idx,
      })),
    };

    const req$ = this.input?.id
      ? this.orderSvc.update(this.input.id, payload as any)
      : this.orderSvc.create(payload as any);

    req$.subscribe({
      next: () => {
        this.message.success(this.input?.id ? 'Order updated.' : 'Order created.');
        this.saving = false;
        this.handleOrderSaved.emit();
      },
      error: () => {
        this.message.error('Failed to save order. Please try again.');
        this.saving = false;
      },
    });
  }

  close(): void { this.onDrawerClosed.emit(); }

  private toOptions(map: Record<number, string>) {
    return Object.entries(map).map(([value, label]) => ({ value: +value, label }));
  }
}

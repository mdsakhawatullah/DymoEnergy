import {
  Component, EventEmitter, Input,
  OnChanges, OnDestroy, OnInit, Output, SimpleChanges,
} from '@angular/core';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { Subject, forkJoin, of } from 'rxjs';
import { catchError, debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs/operators';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { SharedModule } from '../../../shared/shared.module';
import { OrderService } from '../../../proxy/orders/order.service';
import { ProductService } from '../../../proxy/products/product.service';
import { ProductDto } from '../../../proxy/products/models';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import {
  CreateUpdateOrderDto,
  OrderDto,
  OrderItemDto,
  OrderPriorityLabels,
  OrderStageLabels,
  OrderStatusLabels,
} from '../../../proxy/orders/models';
import { TakaPipe, formatTaka, initials, itemKind } from '../../sales-invoices/invoice-format';

/** A person found in earlier orders / invoices, matched by phone (else name). */
interface CustomerMatch {
  key: string;
  name: string;
  phone?: string;
  email?: string;
  address?: string;
  orders: number;
  invoices: number;
}

type DeliveryMode = 'home' | 'install' | 'pickup';

/** OrderShipmentType values used by the three delivery cards. */
const SHIPMENT: Record<DeliveryMode, number> = { home: 5, install: 7, pickup: 4 };

/** Payment chips in counter order; values are OrderPaymentType. */
const PAYMENT_METHODS: { value: number; label: string; mark: string; tone: string; logo?: string }[] = [
  { value: 1,  label: 'Cash',             mark: '৳',   tone: 'green'  },
  { value: 10, label: 'bKash',            mark: 'bK',  tone: 'pink',   logo: 'assets/payment/bkash.png' },
  { value: 11, label: 'Nagad',            mark: 'N',   tone: 'orange', logo: 'assets/payment/nagad.png' },
  { value: 2,  label: 'Card',             mark: 'C',   tone: 'blue',   logo: 'assets/payment/card.svg' },
  { value: 12, label: 'Card EMI',         mark: 'EMI', tone: 'purple', logo: 'assets/payment/card-emi.svg' },
  { value: 4,  label: 'Bank transfer',    mark: 'B',   tone: 'gray'   },
  { value: 8,  label: 'Cash on delivery', mark: 'COD', tone: 'gray'   },
];

/** Where the order came from → CreateMethod (+ CreatedHow text). */
const SOURCES = [
  { key: 'phone',    label: 'Phone',    method: 2 },
  { key: 'whatsapp', label: 'WhatsApp', method: 2 },
  { key: 'showroom', label: 'Showroom', method: 4 },
  { key: 'website',  label: 'Website',  method: 1 },
];

/** All 64 districts, so the delivery address is always tidy and searchable. */
const DISTRICTS = [
  'Bagerhat', 'Bandarban', 'Barguna', 'Barishal', 'Bhola', 'Bogura', 'Brahmanbaria', 'Chandpur', 'Chapai Nawabganj',
  'Chattogram', 'Chuadanga', "Cox's Bazar", 'Cumilla', 'Dhaka', 'Dinajpur', 'Faridpur', 'Feni', 'Gaibandha', 'Gazipur',
  'Gopalganj', 'Habiganj', 'Jamalpur', 'Jashore', 'Jhalokathi', 'Jhenaidah', 'Joypurhat', 'Khagrachhari', 'Khulna',
  'Kishoreganj', 'Kurigram', 'Kushtia', 'Lakshmipur', 'Lalmonirhat', 'Madaripur', 'Magura', 'Manikganj', 'Meherpur',
  'Moulvibazar', 'Munshiganj', 'Mymensingh', 'Naogaon', 'Narail', 'Narayanganj', 'Narsingdi', 'Natore', 'Netrokona',
  'Nilphamari', 'Noakhali', 'Pabna', 'Panchagarh', 'Patuakhali', 'Pirojpur', 'Rajbari', 'Rajshahi', 'Rangamati',
  'Rangpur', 'Satkhira', 'Shariatpur', 'Sherpur', 'Sirajganj', 'Sunamganj', 'Sylhet', 'Tangail', 'Thakurgaon',
];

@Component({
  selector:    'order-entry-drawer',
  templateUrl: './order-entry-drawer.component.html',
  styleUrl:    './order-entry-drawer.component.css',
  imports:     [SharedModule, NzCheckboxModule, TakaPipe],
})
export class OrderEntryDrawerComponent implements OnInit, OnChanges, OnDestroy {

  @Input()  input: OrderDto | null = null;
  @Input()  existingItems: OrderItemDto[] = [];

  @Output() onDrawerClosed   = new EventEmitter<void>();
  @Output() handleOrderSaved = new EventEmitter<void>();

  form!:   FormGroup;
  saving = false;

  // ── Totals (preview; the server stores what we send) ─────────────────────
  subtotal      = 0;
  discountTotal = 0;
  taxTotal      = 0;
  grandTotal    = 0;
  balanceDue    = 0;

  // ── Customer ──────────────────────────────────────────────────────────────
  customer: CustomerMatch | null = null;
  editingCustomer = true;          // true = show the search / new-customer fields
  customerResults: CustomerMatch[] = [];
  customerSearching = false;
  customerDue: { amount: number; invoice?: string; dueDate?: string; count: number } | null = null;
  private customerSearch$ = new Subject<string>();

  // ── Products ─────────────────────────────────────────────────────────────
  productResults: ProductDto[] = [];
  productSearching = false;
  pickedProductId: number | null = null;
  /** Catalogue info for stock hints, by product id. */
  private productInfo = new Map<number, ProductDto>();
  private productSearch$ = new Subject<string>();

  // ── Delivery / payment / details ─────────────────────────────────────────
  delivery: DeliveryMode = 'install';
  district = 'Chattogram';
  area = '';
  street = '';
  showDetails = false;
  sendSms = true;
  source = 'phone';

  readonly paymentMethods = PAYMENT_METHODS;
  readonly sources   = SOURCES;
  readonly districts = DISTRICTS;
  readonly statusOptions   = this.toOptions(OrderStatusLabels);
  readonly stageOptions    = this.toOptions(OrderStageLabels);
  readonly priorityOptions = this.toOptions(OrderPriorityLabels);
  readonly initials = initials;
  readonly itemKind = itemKind;

  /** Product photo for a line, once the catalogue entry is known. */
  imageFor(i: number): string | null {
    const id = this.items.at(i).get('productId')?.value;
    return id ? this.productInfo.get(id)?.primaryImage ?? null : null;
  }

  /** Bootstrap-icons class for the fallback when a product has no photo. */
  iconFor(i: number): string {
    switch (itemKind(this.items.at(i).get('productName')?.value)) {
      case 'panel':    return 'bi-grid-3x3-gap-fill';
      case 'inverter': return 'bi-lightning-charge-fill';
      case 'battery':  return 'bi-battery-charging';
      case 'service':  return 'bi-tools';
      case 'pump':     return 'bi-droplet-fill';
      case 'cable':    return 'bi-plug-fill';
      case 'mount':    return 'bi-bounding-box';
      default:         return 'bi-box-seam';
    }
  }

  private destroy$ = new Subject<void>();

  constructor(
    private fb:         FormBuilder,
    private orderSvc:   OrderService,
    private productSvc: ProductService,
    private invoiceSvc: SalesInvoiceService,
    private message:    NzMessageService,
  ) {
    this.buildForm();
  }

  get isEdit(): boolean { return !!this.input?.id; }

  get statusLabel(): string { return OrderStatusLabels[this.form.get('status')?.value] ?? 'Draft'; }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => this.recalcTotals());

    this.productSearch$.pipe(
      debounceTime(250), distinctUntilChanged(),
      switchMap(q => {
        this.productSearching = true;
        return this.productSvc.getListData({ filter: q || undefined, maxResultCount: 20, skipCount: 0 })
          .pipe(catchError(() => of({ items: [] as ProductDto[], totalCount: 0 })));
      }),
      takeUntil(this.destroy$),
    ).subscribe(r => {
      this.productResults = r.items;
      r.items.forEach(p => this.productInfo.set(p.id, p));
      this.productSearching = false;
    });
    this.productSearch$.next('');

    this.customerSearch$.pipe(
      debounceTime(300), distinctUntilChanged(),
      switchMap(q => {
        if (q.trim().length < 2) return of([] as CustomerMatch[]);
        this.customerSearching = true;
        return forkJoin({
          orders:   this.orderSvc.getListData({ filter: q, maxResultCount: 30 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
          invoices: this.invoiceSvc.getListData({ filter: q, maxResultCount: 30 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
        }).pipe(switchMap(r => of(this.mergeCustomers(r.orders.items as OrderDto[], r.invoices.items))));
      }),
      takeUntil(this.destroy$),
    ).subscribe(list => {
      this.customerResults = list;
      this.customerSearching = false;
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['input']) {
      if (this.input) this.loadForEdit(this.input);
      else this.resetForCreate();
      this.recalcTotals();
    }

    if (changes['existingItems'] && this.existingItems?.length) {
      this.items.clear();
      this.existingItems
        .slice().sort((a, b) => a.displayOrder - b.displayOrder)
        .forEach(item => this.items.push(this.buildItemGroup(item)));
      this.loadProductInfo(this.existingItems.map(i => i.productId).filter((id): id is number => !!id));
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
      customerId:        [null],
      customerName:      [null],
      customerEmail:     [null],
      customerPhone:     [null],
      customerReference: [null],
      billingAddress:    [null],
      deliveryContact:   [null],
      deliveryPhone:     [null],
      installTeam:       [null],
      status:       [1],
      stage:        [1],
      priority:     [2],
      paymentType:  [10],
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
      // Line discount / VAT are kept (and recalculated) but not shown — edit them in "More details"
      discountPercent: [item?.discountPercent ?? 0],
      discountAmount:  [item?.discountAmount  ?? 0],
      taxRate:         [item?.taxRate         ?? 0],
      taxAmount:       [item?.taxAmount       ?? 0],
      lineTotal:       [item?.lineTotal       ?? 0],
      displayOrder:    [item?.displayOrder    ?? 0],
    });
  }

  private resetForCreate(): void {
    this.form.reset({
      status: 1, stage: 1, priority: 2, paymentType: 10,
      currencyCode: 'BDT', orderDate: new Date(),
      voucherAmount: 0, taxRate: 0, shippingCost: 0, amountPaid: 0,
    }, { emitEvent: false });
    this.items.clear();
    this.customer = null;
    this.customerDue = null;
    this.editingCustomer = true;
    this.delivery = 'install';
    this.district = 'Chattogram';
    this.area = this.street = '';
    this.source = 'phone';
    this.sendSms = true;
  }

  private loadForEdit(o: OrderDto): void {
    this.form.patchValue({
      orderDate:             o.orderDate ? new Date(o.orderDate) : new Date(),
      estimatedDeliveryDate: o.estimatedDeliveryDate ? new Date(o.estimatedDeliveryDate) : null,
      actualDeliveryDate:    o.actualDeliveryDate ? new Date(o.actualDeliveryDate) : null,
      customerId:        o.customerId,
      customerName:      o.customerName,
      customerEmail:     o.customerEmail,
      customerPhone:     o.customerPhone,
      customerReference: o.customerReference,
      billingAddress:    o.billingAddress,
      deliveryContact:   o.deliveryContact,
      deliveryPhone:     o.deliveryPhone,
      installTeam:       o.installTeam,
      status:       o.status,
      stage:        o.stage,
      priority:     o.priority,
      paymentType:  o.paymentType ?? null,
      voucherCode:   o.voucherCode,
      voucherAmount: o.voucherAmount,
      currencyCode:  o.currencyCode,
      taxRate:       o.taxRate,
      shippingCost:  o.shippingCost,
      paymentDate:   o.paymentDate ? new Date(o.paymentDate) : null,
      amountPaid:    o.amountPaid,
      notes:         o.notes,
      notesInvoice:  o.notesInvoice,
      terms:         o.terms,
      internalNotes: o.internalNotes,
    }, { emitEvent: false });

    this.delivery = o.shipmentType === SHIPMENT.pickup ? 'pickup' : o.shipmentType === SHIPMENT.install ? 'install' : 'home';
    this.parseAddress(o.deliveryAddress);
    this.source = o.createdHow === 'WhatsApp' ? 'whatsapp'
      : SOURCES.find(s => s.method === o.createMethod && s.key !== 'whatsapp')?.key ?? 'phone';
    this.sendSms = false;

    if (o.customerName || o.customerPhone) {
      this.selectCustomer({
        key: o.customerPhone ?? o.customerName ?? '', name: o.customerName ?? '', phone: o.customerPhone,
        email: o.customerEmail, address: o.deliveryAddress, orders: 0, invoices: 0,
      }, false);
    } else {
      this.customer = null;
      this.editingCustomer = true;
    }
  }

  // ── Customer ──────────────────────────────────────────────────────────────

  onCustomerSearch(q: string): void { this.customerSearch$.next(q); }

  /** Group earlier orders and invoices into distinct people. */
  private mergeCustomers(orders: OrderDto[], invoices: { customerName?: string; customerPhone?: string; customerEmail?: string; billingAddress?: string }[]): CustomerMatch[] {
    const map = new Map<string, CustomerMatch>();
    const key = (phone?: string, name?: string) => {
      const d = (phone ?? '').replace(/\D/g, '');
      return d.length >= 10 ? d.slice(-10) : (name ?? '').trim().toLowerCase();
    };
    const add = (name: string | undefined, phone: string | undefined, email: string | undefined, address: string | undefined, kind: 'orders' | 'invoices') => {
      if (!name && !phone) return;
      const k = key(phone, name);
      const m = map.get(k) ?? { key: k, name: name ?? '', phone, email, address, orders: 0, invoices: 0 };
      m.name ||= name ?? ''; m.phone ||= phone; m.email ||= email; m.address ||= address;
      m[kind]++;
      map.set(k, m);
    };
    orders.forEach(o => add(o.customerName, o.customerPhone, o.customerEmail, o.deliveryAddress ?? o.billingAddress, 'orders'));
    invoices.forEach(i => add(i.customerName, i.customerPhone, i.customerEmail, i.billingAddress, 'invoices'));
    return [...map.values()].sort((a, b) => (b.orders + b.invoices) - (a.orders + a.invoices)).slice(0, 8);
  }

  selectCustomer(c: CustomerMatch, fillAddress = true): void {
    this.customer = c;
    this.editingCustomer = false;
    this.customerResults = [];
    this.form.patchValue({ customerName: c.name, customerPhone: c.phone ?? null, customerEmail: c.email ?? null });
    if (fillAddress && c.address && !this.street) this.parseAddress(c.address);
    this.loadCustomerHistory(c);
  }

  /** Start a brand-new customer from what was typed in the search box. */
  newCustomer(typed: string): void {
    const isPhone = /^[+\d][\d\s-]{6,}$/.test(typed.trim());
    this.customer = null;
    this.editingCustomer = true;
    this.customerResults = [];
    this.form.patchValue({ customerName: isPhone ? null : typed.trim() || null, customerPhone: isPhone ? typed.trim() : null });
  }

  confirmNewCustomer(): void {
    const v = this.form.value;
    if (!v.customerName?.trim() && !v.customerPhone?.trim()) { this.message.warning('Enter a name or phone number.'); return; }
    this.customer = { key: v.customerPhone ?? v.customerName, name: v.customerName ?? '', phone: v.customerPhone ?? undefined, email: v.customerEmail ?? undefined, orders: 0, invoices: 0 };
    this.editingCustomer = false;
    this.customerDue = null;
  }

  changeCustomer(): void {
    this.editingCustomer = true;
    this.customerDue = null;
  }

  /** Order count and any money still owed on invoices, for the warning strip. */
  private loadCustomerHistory(c: CustomerMatch): void {
    const q = c.phone || c.name;
    if (!q) return;
    forkJoin({
      orders:   this.orderSvc.getListData({ filter: q, maxResultCount: 100 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      invoices: this.invoiceSvc.getListData({ filter: q, maxResultCount: 100 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
    }).subscribe(({ orders, invoices }) => {
      if (this.customer !== c) return;
      const others = (orders.items as OrderDto[]).filter(o => o.id !== this.input?.id);
      c.orders   = others.length;
      c.invoices = invoices.items.length;
      const open = invoices.items.filter(i => i.balanceDue > 0.5 && ![1, 6, 7].includes(i.status))
        .sort((a, b) => (a.dueDate ?? '').localeCompare(b.dueDate ?? ''));
      this.customerDue = open.length
        ? { amount: open.reduce((s, i) => s + i.balanceDue, 0), invoice: open[0].invoiceNumber, dueDate: open[0].dueDate, count: open.length }
        : null;
    });
  }

  get customerMeta(): string {
    const c = this.customer;
    if (!c) return '';
    const area = this.area || c.address?.split(',').slice(-2, -1)[0]?.trim();
    const history = c.orders + c.invoices;
    return [c.phone, [area, this.district].filter(Boolean).join(', '), history ? `${history} earlier purchase${history === 1 ? '' : 's'}` : 'New customer']
      .filter(Boolean).join(' · ');
  }

  // ── Products ─────────────────────────────────────────────────────────────

  onProductSearch(q: string): void { this.productSearch$.next(q); }

  onProductPicked(id: number | null): void {
    const p = this.productResults.find(x => x.id === id);
    if (!p) return;
    // Picking the same product again just adds one more
    const existing = this.items.controls.find(c => c.get('productId')?.value === p.id);
    if (existing) {
      existing.get('quantity')!.setValue((+existing.get('quantity')!.value || 0) + 1);
    } else {
      this.items.push(this.buildItemGroup({
        productId: p.id, productName: p.name, sku: p.sku, quantity: 1,
        unitPrice: p.discountPrice && p.discountPrice > 0 ? p.discountPrice : p.price,
      }));
    }
    this.productInfo.set(p.id, p);
    setTimeout(() => this.pickedProductId = null);
  }

  addCustomLine(): void {
    this.items.push(this.buildItemGroup({ productName: '', quantity: 1, unitPrice: 0 }));
  }

  removeItem(i: number): void { this.items.removeAt(i); }

  step(i: number, delta: number): void {
    const q = this.items.at(i).get('quantity')!;
    q.setValue(Math.max(1, (+q.value || 0) + delta));
  }

  /** "৳98,000 · 7 left", "৳17,500 · 116 in stock", "৳2,500 · Service". */
  lineMeta(i: number): { text: string; warn: boolean } {
    const row = this.items.at(i).value;
    const price = formatTaka(+row.unitPrice || 0);
    const p = row.productId ? this.productInfo.get(row.productId) : null;
    if (!p) return { text: row.sku ? `${price} · ${row.sku}` : `${price} · Service`, warn: false };
    const stock = p.stockQuantity ?? 0;
    if (stock <= 0) return { text: `${price} · out of stock`, warn: true };
    if ((+row.quantity || 0) > stock) return { text: `${price} · only ${stock} in stock`, warn: true };
    return { text: `${price} · ${stock <= 10 ? `${stock} left` : `${stock} in stock`}`, warn: stock <= 10 };
  }

  private loadProductInfo(ids: number[]): void {
    const missing = [...new Set(ids)].filter(id => !this.productInfo.has(id));
    if (!missing.length) return;
    forkJoin(missing.map(id => this.productSvc.get(id).pipe(catchError(() => of(null)))))
      .subscribe(list => list.forEach(p => p && this.productInfo.set(p.id, p)));
  }

  // ── Delivery ─────────────────────────────────────────────────────────────

  setDelivery(mode: DeliveryMode): void { this.delivery = mode; }

  /** "House 12, Road 3, Halishahar, Chattogram" → street / area / district. */
  private parseAddress(address?: string | null): void {
    const parts = (address ?? '').split(',').map(p => p.trim()).filter(Boolean);
    const last = parts[parts.length - 1];
    if (last && DISTRICTS.includes(last)) {
      this.district = last;
      parts.pop();
      this.area = parts.length > 1 ? parts.pop()! : '';
    } else {
      this.area = '';
    }
    this.street = parts.join(', ');
  }

  private composeAddress(): string | undefined {
    if (this.delivery === 'pickup') return undefined;
    return [this.street.trim(), this.area.trim(), this.district].filter(Boolean).join(', ') || undefined;
  }

  // ── Payment ──────────────────────────────────────────────────────────────

  setPayment(v: number): void { this.form.get('paymentType')!.setValue(v); }

  payInFull(): void { this.form.get('amountPaid')!.setValue(Math.round(this.grandTotal)); }

  // ── Totals ───────────────────────────────────────────────────────────────

  recalcTotals(): void {
    let sub = 0, disc = 0, tax = 0;
    this.items.controls.forEach(ctrl => {
      const r    = ctrl.value;
      const gross = (+r.quantity || 0) * (+r.unitPrice || 0);
      const d    = gross * (+r.discountPercent || 0) / 100;
      const t    = (gross - d) * (+r.taxRate || 0) / 100;
      ctrl.patchValue({ discountAmount: +d.toFixed(2), taxAmount: +t.toFixed(2), lineTotal: +(gross - d + t).toFixed(2) }, { emitEvent: false });
      sub += gross; disc += d; tax += t;
    });

    const shipping = +(this.form.get('shippingCost')?.value  || 0);
    const voucher  = +(this.form.get('voucherAmount')?.value || 0);
    const paid     = +(this.form.get('amountPaid')?.value    || 0);
    const grand    = Math.max(0, sub - disc + tax + shipping - voucher);

    this.subtotal      = +sub.toFixed(2);
    this.discountTotal = +disc.toFixed(2);
    this.taxTotal      = +tax.toFixed(2);
    this.grandTotal    = +grand.toFixed(2);
    this.balanceDue    = +Math.max(0, grand - paid).toFixed(2);
  }

  get productsTotal(): number { return this.subtotal - this.discountTotal + this.taxTotal; }

  // ── Save ─────────────────────────────────────────────────────────────────

  save(): void {
    const v = this.form.value;
    if (!v.customerName?.trim() && !v.customerPhone?.trim()) { this.message.warning('Add a customer first.'); return; }
    if (this.items.length === 0) { this.message.warning('Add at least one product.'); return; }
    if ((v.items as any[]).some(r => !r.productName?.trim())) { this.message.warning('Every line needs a name.'); return; }
    if (this.delivery !== 'pickup' && !this.street.trim() && !this.area.trim()) { this.message.warning('Add the delivery address.'); return; }
    if ((+v.amountPaid || 0) > this.grandTotal + 0.5) { this.message.warning('Amount received is more than the order total.'); return; }

    const toIso  = (d: unknown) => d instanceof Date ? d.toISOString() : (d as string) ?? undefined;
    const source = SOURCES.find(s => s.key === this.source)!;
    const paid   = +v.amountPaid || 0;
    const address = this.composeAddress();

    const payload: CreateUpdateOrderDto = {
      orderDate:             toIso(v.orderDate) ?? new Date().toISOString(),
      estimatedDeliveryDate: this.delivery === 'pickup' ? undefined : toIso(v.estimatedDeliveryDate),
      actualDeliveryDate:    toIso(v.actualDeliveryDate),
      customerId:        v.customerId ?? undefined,
      customerName:      v.customerName?.trim(),
      customerEmail:     v.customerEmail?.trim() || undefined,
      customerPhone:     v.customerPhone?.trim(),
      customerReference: v.customerReference,
      billingAddress:    v.billingAddress || address,
      deliveryAddress:   address,
      deliveryContact:   v.deliveryContact,
      deliveryPhone:     v.deliveryPhone,
      installTeam:       this.delivery === 'install' ? v.installTeam?.trim() || undefined : undefined,
      status:       v.status,
      stage:        v.stage,
      priority:     v.priority,
      shipmentType: SHIPMENT[this.delivery],
      paymentType:  v.paymentType ?? undefined,
      createMethod: source.method,
      createdHow:   source.label,
      voucherCode:   v.voucherCode,
      voucherAmount: +v.voucherAmount || 0,
      currencyCode:  v.currencyCode || 'BDT',
      taxRate:       +v.taxRate || 0,
      shippingCost:  this.delivery === 'pickup' ? 0 : +v.shippingCost || 0,
      subtotal:      this.subtotal,
      discountTotal: this.discountTotal,
      taxTotal:      this.taxTotal,
      grandTotal:    this.grandTotal,
      amountPaid:    paid,
      balanceDue:    this.balanceDue,
      paymentDate:   paid > 0 ? toIso(v.paymentDate) ?? new Date().toISOString() : toIso(v.paymentDate),
      notes:         v.notes,
      notesInvoice:  v.notesInvoice,
      terms:         v.terms,
      internalNotes: v.internalNotes,
      items: (v.items as any[]).map((row, idx) => ({
        id:              row.id ?? undefined,
        productId:       row.productId ?? undefined,
        productName:     row.productName?.trim(),
        sku:             row.sku,
        description:     row.description,
        quantity:        +row.quantity || 1,
        unitPrice:       +row.unitPrice || 0,
        discountPercent: +row.discountPercent || 0,
        discountAmount:  +row.discountAmount || 0,
        taxRate:         +row.taxRate || 0,
        taxAmount:       +row.taxAmount || 0,
        lineTotal:       +row.lineTotal || 0,
        displayOrder:    idx,
      })),
    } as CreateUpdateOrderDto;

    this.saving = true;
    const req$ = this.isEdit ? this.orderSvc.update(this.input!.id, payload) : this.orderSvc.create(payload);

    req$.subscribe({
      next: saved => {
        this.saving = false;
        this.message.success(this.isEdit ? 'Order updated.' : `Order ${saved?.orderNumber ?? ''} created.`);
        if (!this.isEdit && this.sendSms && payload.customerPhone) this.openSmsConfirmation(saved, payload);
        this.handleOrderSaved.emit();
      },
      error: () => { this.saving = false; },
    });
  }

  /** Opens the SMS app with a ready confirmation — there is no SMS gateway to send it for us. */
  private openSmsConfirmation(saved: OrderDto | null, p: CreateUpdateOrderDto): void {
    let digits = (p.customerPhone ?? '').replace(/\D/g, '');
    if (digits.length === 11 && digits.startsWith('0')) digits = '88' + digits;
    const when = p.estimatedDeliveryDate
      ? ` ${this.delivery === 'install' ? 'Installation' : 'Delivery'} on ${new Date(p.estimatedDeliveryDate).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })}.`
      : '';
    const text = `Dear ${p.customerName ?? 'customer'}, your DymoEnergy order ${saved?.orderNumber ?? ''} is confirmed. ` +
      `Total ${formatTaka(p.grandTotal)}${p.balanceDue > 0 ? `, due ${formatTaka(p.balanceDue)}` : ', fully paid'}.${when} Thank you!`;
    window.location.href = `sms:+${digits}?body=${encodeURIComponent(text)}`;
  }

  close(): void { this.onDrawerClosed.emit(); }

  private toOptions(map: Record<number, string>) {
    return Object.entries(map).map(([value, label]) => ({ value: +value, label }));
  }
}

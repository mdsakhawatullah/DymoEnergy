import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { FinanceService } from '../../../proxy/finance/finance.service';
import { SalesInvoiceService } from '../../../proxy/sales-invoices/sales-invoice.service';
import { SalesInvoicePaymentMethodType } from '../../../proxy/sales-invoices/models';
import {
  FinanceAccountDto,
  FinanceBillDto,
  FinanceCategoryDto,
  FinanceDirection,
  FinanceDueDto,
  FinanceExpenseDto,
  FinanceItemDto,
  FinanceOverviewDto,
  FinanceRecurringDto,
} from '../../../proxy/finance/models';
import { PAYMENT_METHODS, fmtFull } from '../finance.utils';
import { parseDateKey, toDateKey } from '../../project-planning/project-planning.utils';

export type RecordMode = 'menu' | 'customer' | 'supplier' | 'bill' | 'expense' | 'move' | 'transfer' | 'receive' | 'recurring';

export interface RecordDrawerInput {
  mode: RecordMode;
  invoiceId?: number;
  billId?: number;
  bill?: FinanceBillDto | null;
  expense?: FinanceExpenseDto | null;
  item?: FinanceItemDto;
  recurring?: FinanceRecurringDto;
}

type MenuTab = 'customer' | 'supplier' | 'expense' | 'move' | 'transfer';

@Component({
  selector: 'finance-record-drawer',
  templateUrl: './record-drawer.component.html',
  styleUrl: './record-drawer.component.css',
  imports: [SharedModule],
})
export class RecordDrawerComponent implements OnChanges {
  readonly Direction = FinanceDirection;
  readonly methods = PAYMENT_METHODS;

  @Input({ required: true }) input!: RecordDrawerInput;
  @Input({ required: true }) overview!: FinanceOverviewDto;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  menu: { key: MenuTab; label: string }[] = [
    { key: 'customer', label: 'Customer payment' },
    { key: 'supplier', label: 'Pay a supplier' },
    { key: 'expense', label: 'Expense' },
    { key: 'move', label: 'Other money' },
    { key: 'transfer', label: 'Transfer' },
  ];
  menuIndex = 0;
  saving = false;
  uploading = false;

  openInvoices: FinanceDueDto[] = [];
  openBills: FinanceBillDto[] = [];

  // shared
  date: Date = new Date();
  accountId: number | null = null;
  amount: number | null = null;
  reference = '';
  note = '';

  // customer payment
  invoiceId: number | null = null;
  methodValue = 1;

  // supplier payment & bill
  billId: number | null = null;
  supplier = '';
  description = '';
  billNumber = '';
  billDate: Date = new Date();
  dueDate: Date = new Date();
  categoryId: number | null = null;
  receiptUrl: string | null = null;
  editingBill: FinanceBillDto | null = null;

  // expense
  editingExpense: FinanceExpenseDto | null = null;
  paidByNote = '';

  // other money
  direction: FinanceDirection = FinanceDirection.In;
  categoryText = '';

  // transfer
  toAccountId: number | null = null;

  constructor(
    private api: FinanceService,
    private invoices: SalesInvoiceService,
    private message: NzMessageService,
  ) {}

  get accounts(): FinanceAccountDto[] {
    return this.overview.accounts;
  }

  get categories(): FinanceCategoryDto[] {
    return this.overview.categories;
  }

  get symbol(): string {
    return this.overview.setting.currencySymbol;
  }

  money = (v: number) => fmtFull(v, this.symbol);

  /** The form to show: the single mode, or the selected tab of the menu. */
  get mode(): RecordMode {
    return this.input.mode === 'menu' ? this.menu[this.menuIndex].key : this.input.mode;
  }

  get title(): string {
    switch (this.mode) {
      case 'customer': return 'Record a customer payment';
      case 'supplier': return 'Pay a supplier bill';
      case 'bill': return this.editingBill ? 'Supplier bill' : 'New supplier bill';
      case 'expense': return this.editingExpense ? 'Edit expense' : 'Record an expense';
      case 'move': return 'Other money in or out';
      case 'transfer': return 'Move money between accounts';
      case 'receive': return 'Money has arrived';
      default: return 'Pay this month’s cost';
    }
  }

  ngOnChanges(): void {
    const i = this.input;
    const firstActive = this.accounts[0]?.id ?? null;
    this.accountId = firstActive;
    this.toAccountId = this.accounts[1]?.id ?? null;
    this.date = new Date();
    this.amount = null;
    this.reference = '';
    this.note = '';
    this.editingBill = i.bill ?? null;
    this.editingExpense = i.expense ?? null;
    this.menuIndex = 0;

    if (i.mode === 'receive' && i.item) {
      this.amount = i.item.amount;
      this.accountId = this.accounts.find(a => a.shortCode.toLowerCase() === (i.item!.extra ?? '').toLowerCase())?.id ?? firstActive;
    }
    if (i.mode === 'recurring' && i.recurring) {
      this.amount = i.recurring.amount;
      this.accountId = i.recurring.accountId ?? firstActive;
    }
    if (i.mode === 'bill') this.loadBill(i.bill ?? null);
    if (i.mode === 'expense') this.loadExpense(i.expense ?? null);

    this.loadLists(i);
  }

  private loadLists(i: RecordDrawerInput): void {
    if (i.mode === 'menu' || i.mode === 'customer') {
      this.api.getDues('this-month').subscribe(d => {
        this.openInvoices = d.dues;
        if (i.invoiceId) this.pickInvoice(i.invoiceId);
      });
    }
    if (i.mode === 'menu' || i.mode === 'supplier') {
      this.api.getBills('this-month').subscribe(b => {
        this.openBills = b.bills.filter(x => x.remaining > 0);
        if (i.billId) this.pickBill(i.billId);
      });
    }
  }

  onMenuChange(): void {
    this.amount = null;
    this.reference = '';
    this.note = '';
    this.paidByNote = '';
    this.editingExpense = null;
    this.editingBill = null;
    this.loadExpense(null);
  }

  // ── Customer payment ──────────────────────────────────────────────────────
  pickInvoice(id: number | null): void {
    this.invoiceId = id;
    const inv = this.openInvoices.find(x => x.invoiceId === id);
    if (inv) this.amount = inv.balance;
  }

  /** Where this payment method's money will show up. */
  get methodAccount(): FinanceAccountDto | undefined {
    const name = this.methods.find(m => m.value === this.methodValue)?.name;
    return this.accounts.find(a => a.paymentMethods.includes(name ?? ''));
  }

  saveCustomer(): void {
    const inv = this.openInvoices.find(x => x.invoiceId === this.invoiceId);
    if (!inv || !this.amount) return void this.message.warning('Choose an invoice and an amount.');
    this.run(this.invoices.collectPayment(inv.invoiceId, {
      amount: this.amount, method: this.methodValue as SalesInvoicePaymentMethodType, paidOn: this.dateText(this.date), referenceNumber: this.reference || undefined, note: this.note || undefined,
    }), 'Payment recorded.');
  }

  // ── Supplier payment ──────────────────────────────────────────────────────
  pickBill(id: number | null): void {
    this.billId = id;
    const b = this.openBills.find(x => x.id === id);
    if (b) this.amount = b.remaining;
  }

  get selectedBill(): FinanceBillDto | undefined {
    return this.openBills.find(b => b.id === this.billId);
  }

  saveSupplierPayment(): void {
    if (!this.billId || !this.amount || !this.accountId) return void this.message.warning('Choose a bill, an account and an amount.');
    this.run(this.api.payBill(this.billId, { amount: this.amount, accountId: this.accountId, date: this.dateText(this.date), reference: this.reference || null }), 'Payment recorded.');
  }

  // ── Bill ──────────────────────────────────────────────────────────────────
  private loadBill(b: FinanceBillDto | null): void {
    this.supplier = b?.supplier ?? '';
    this.description = b?.description ?? '';
    this.billNumber = b?.billNumber ?? '';
    this.billDate = b ? parseDateKey(b.billDate.slice(0, 10)) : new Date();
    this.dueDate = b ? parseDateKey(b.dueDate.slice(0, 10)) : new Date(Date.now() + 14 * 86400000);
    this.amount = b?.amount ?? null;
    this.categoryId = b?.categoryId ?? this.categories.find(c => c.name === 'Product cost')?.id ?? this.categories[0]?.id ?? null;
    this.receiptUrl = b?.receiptUrl ?? null;
    this.note = b?.note ?? '';
  }

  saveBill(): void {
    if (!this.supplier.trim() || !this.amount || !this.categoryId) return void this.message.warning('Enter the supplier, the amount and a category.');
    const body = {
      supplier: this.supplier, description: this.description || null, billNumber: this.billNumber || null,
      billDate: this.dateText(this.billDate), dueDate: this.dateText(this.dueDate), amount: this.amount,
      categoryId: this.categoryId, receiptUrl: this.receiptUrl, note: this.note || null,
    };
    this.run(this.editingBill ? this.api.updateBill(this.editingBill.id, body) : this.api.createBill(body), 'Bill saved.');
  }

  deleteBill(): void {
    if (this.editingBill) this.run(this.api.deleteBill(this.editingBill.id), 'Bill deleted.');
  }

  deleteBillPayment(paymentId: number): void {
    if (!this.editingBill) return;
    this.api.deleteBillPayment(this.editingBill.id, paymentId).subscribe(b => {
      this.editingBill = b;
      this.message.success('Payment removed.');
    });
  }

  // ── Expense ───────────────────────────────────────────────────────────────
  private loadExpense(e: FinanceExpenseDto | null): void {
    this.date = e ? parseDateKey(e.date.slice(0, 10)) : new Date();
    this.description = e?.description ?? '';
    this.amount = e?.amount ?? null;
    this.categoryId = e?.categoryId ?? this.categories.find(c => c.costGroup === 2)?.id ?? this.categories[0]?.id ?? null;
    this.accountId = e ? e.accountId ?? null : this.accounts[0]?.id ?? null;
    this.paidByNote = e?.paidByNote ?? '';
    this.receiptUrl = e?.receiptUrl ?? null;
  }

  saveExpense(): void {
    if (!this.description.trim() || !this.amount || !this.categoryId) return void this.message.warning('Enter what it was for, the amount and a category.');
    const body = {
      date: this.dateText(this.date), categoryId: this.categoryId, description: this.description, amount: this.amount,
      accountId: this.accountId, paidByNote: this.paidByNote || null, receiptUrl: this.receiptUrl,
    };
    this.run(this.editingExpense ? this.api.updateExpense(this.editingExpense.id, body) : this.api.createExpense(body), 'Expense saved.');
  }

  deleteExpense(): void {
    if (this.editingExpense) this.run(this.api.deleteExpense(this.editingExpense.id), 'Expense deleted.');
  }

  // ── Other money & transfer ────────────────────────────────────────────────
  saveMove(): void {
    if (!this.accountId || !this.amount) return void this.message.warning('Choose an account and an amount.');
    this.run(this.api.createTransaction({
      direction: this.direction, accountId: this.accountId, amount: this.amount, date: this.dateText(this.date),
      category: this.categoryText || null, description: this.description || null, reference: this.reference || null,
    }), 'Recorded.');
  }

  saveTransfer(): void {
    if (!this.accountId || !this.toAccountId || !this.amount) return void this.message.warning('Choose both accounts and an amount.');
    this.run(this.api.createTransfer({ fromAccountId: this.accountId, toAccountId: this.toAccountId, amount: this.amount, date: this.dateText(this.date), note: this.note || null }), 'Transfer recorded.');
  }

  // ── Receive & recurring ───────────────────────────────────────────────────
  saveReceive(): void {
    if (!this.input.item || !this.accountId) return;
    this.run(this.api.receiveItem(this.input.item.id, { accountId: this.accountId, date: this.dateText(this.date) }), 'Money received.');
  }

  saveRecurring(): void {
    if (!this.input.recurring) return;
    this.run(this.api.payRecurring(this.input.recurring.id, { date: this.dateText(this.date), accountId: this.accountId, amount: this.amount }), 'Cost recorded.');
  }

  // ── Receipt upload ────────────────────────────────────────────────────────
  uploadReceipt(ev: Event): void {
    const el = ev.target as HTMLInputElement;
    const file = el.files?.[0];
    el.value = '';
    if (!file) return;
    this.uploading = true;
    this.api.uploadReceipt(file).subscribe({
      next: r => {
        this.receiptUrl = r.url;
        this.uploading = false;
      },
      error: err => {
        this.uploading = false;
        this.message.error(err?.error?.message ?? 'Upload failed.');
      },
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  private dateText(d: Date): string {
    return toDateKey(d);
  }

  private run(req$: Observable<unknown>, success: string): void {
    this.saving = true;
    req$.subscribe({
      next: () => {
        this.saving = false;
        this.message.success(success);
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }

  accountLabel(a: FinanceAccountDto): string {
    return `${a.name} · ${this.money(a.balance)}`;
  }
}

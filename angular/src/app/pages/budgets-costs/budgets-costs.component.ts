import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { FinanceService } from '../../proxy/finance/finance.service';
import {
  FinanceAccountDto,
  FinanceBarDto,
  FinanceBillDto,
  FinanceBillsDto,
  FinanceCashDto,
  FinanceDealerDto,
  FinanceDirection,
  FinanceDueDto,
  FinanceDuesDto,
  FinanceExpenseDto,
  FinanceExpensesDto,
  FinanceInsightDto,
  FinanceItemDto,
  FinanceOverviewDto,
  FinancePnlDto,
  FinancePnlLineDto,
  FinanceRecurringDto,
  FinanceSettingDto,
} from '../../proxy/finance/models';
import { RecordDrawerComponent, RecordDrawerInput } from './record-drawer/record-drawer.component';
import { FinanceCustomizeDrawerComponent, FinanceCustomizeTab } from './customize-drawer/customize-drawer.component';
import { fill, fmtChange, fmtCompact, fmtFull } from './finance.utils';
import { shortDate, tint } from '../project-planning/project-planning.utils';

type TabKey = 'cash' | 'dues' | 'bills' | 'expenses' | 'pnl';

const FALLBACK_SETTING: FinanceSettingDto = {
  accentColor: '#0E6B3F', currencySymbol: '৳', compactMoney: true, dueSoonDays: 7, agingStep1: 30, agingStep2: 60, chartMonths: 6, labels: [],
};

@Component({
  selector: 'app-budgets-costs',
  templateUrl: './budgets-costs.component.html',
  styleUrl: './budgets-costs.component.css',
  imports: [SharedModule, RecordDrawerComponent, FinanceCustomizeDrawerComponent],
})
export class BudgetsCostsComponent implements OnInit {
  readonly Direction = FinanceDirection;

  tab: TabKey = 'cash';
  period = 'this-month';
  loading = false;
  exporting = false;

  overview: FinanceOverviewDto | null = null;
  cash: FinanceCashDto | null = null;
  dues: FinanceDuesDto | null = null;
  bills: FinanceBillsDto | null = null;
  expenses: FinanceExpensesDto | null = null;
  pnl: FinancePnlDto | null = null;

  // drawers
  recordOpen = false;
  recordInput: RecordDrawerInput = { mode: 'menu' };
  customizeOpen = false;
  customizeTab: FinanceCustomizeTab = 'page';
  customizeChanged = false;
  statementDealer: FinanceDealerDto | null = null;

  constructor(
    private api: FinanceService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  // ── Settings, labels, money ───────────────────────────────────────────────
  get setting(): FinanceSettingDto {
    return this.overview?.setting ?? FALLBACK_SETTING;
  }

  /** Customisable text; `{token}`s are filled from vars, and `key.one` is used when n === 1 and it exists. */
  t(key: string, vars: Record<string, string | number> = {}): string {
    const labels = this.setting.labels;
    const one = vars['n'] === 1 ? labels.find(l => l.key === key + '.one') : undefined;
    return fill((one ?? labels.find(l => l.key === key))?.value ?? '', vars);
  }

  m = (v: number | null | undefined) => fmtFull(v, this.setting.currencySymbol);
  k = (v: number | null | undefined) => fmtCompact(v, this.setting);
  change = fmtChange;
  shortDate = shortDate;
  tint = tint;

  get accent(): string {
    return this.setting.accentColor;
  }

  get periodLabel(): string {
    return this.overview?.period.label ?? '';
  }

  // ── Loading ───────────────────────────────────────────────────────────────
  load(): void {
    this.loading = true;
    this.api.getOverview(this.period).subscribe({
      next: o => {
        this.overview = o;
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
    this.loadTab();
  }

  private loadTab(): void {
    const p = this.period;
    switch (this.tab) {
      case 'cash': this.api.getCash(p).subscribe(x => (this.cash = x)); break;
      case 'dues': this.api.getDues(p).subscribe(x => (this.dues = x)); break;
      case 'bills': this.api.getBills(p).subscribe(x => (this.bills = x)); break;
      case 'expenses': this.api.getExpenses(p).subscribe(x => (this.expenses = x)); break;
      case 'pnl': this.api.getPnl(p).subscribe(x => (this.pnl = x)); break;
    }
  }

  setTab(tab: TabKey): void {
    this.tab = tab;
    this.loadTab();
  }

  onPeriodChange(): void {
    this.load();
  }

  get tabs(): { key: TabKey; label: string; icon: string; badge: number; tone: string }[] {
    const o = this.overview;
    return [
      { key: 'cash', label: this.t('tab.cash'), icon: 'bi-wallet2', badge: 0, tone: '' },
      { key: 'dues', label: this.t('tab.dues'), icon: 'bi-person-lines-fill', badge: o?.lateInvoices ?? 0, tone: 'amber' },
      { key: 'bills', label: this.t('tab.bills'), icon: 'bi-receipt', badge: o?.overdueBills ?? 0, tone: 'red' },
      { key: 'expenses', label: this.t('tab.expenses'), icon: 'bi-bar-chart', badge: 0, tone: '' },
      { key: 'pnl', label: this.t('tab.pnl'), icon: 'bi-graph-up', badge: 0, tone: '' },
    ];
  }

  get cards(): { key: TabKey; label: string; value: string; note: string; dot: string; valueClass: string }[] {
    const o = this.overview;
    if (!o) return [];
    return [
      { key: 'cash', label: this.t('kpi.way'), value: this.k(o.onTheWayTotal), dot: '#2563EB', valueClass: '', note: this.t('kpi.wayNote') },
      { key: 'dues', label: this.t('kpi.owe'), value: this.k(o.customersOwe), dot: '#D97706', valueClass: 'is-amber', note: this.t('kpi.oweNote', { n: o.lateInvoices }) },
      { key: 'bills', label: this.t('kpi.suppliers'), value: this.k(o.suppliersOwe), dot: '#B42318', valueClass: 'is-red', note: this.t('kpi.suppliersNote', { n: o.overdueBills }) },
      { key: 'pnl', label: this.t('kpi.profit', { period: o.profitPeriodLabel }), value: this.k(o.profit), dot: '#0E6B3F', valueClass: o.profit < 0 ? 'is-red' : 'is-green', note: this.t('kpi.profitNote') },
    ];
  }

  // ── Header actions ────────────────────────────────────────────────────────
  openRecord(input: RecordDrawerInput = { mode: 'menu' }): void {
    this.recordInput = input;
    this.recordOpen = true;
  }

  onRecorded(): void {
    this.recordOpen = false;
    this.load();
  }

  openCustomize(tab: FinanceCustomizeTab = 'page'): void {
    this.customizeTab = tab;
    this.customizeChanged = false;
    this.customizeOpen = true;
  }

  closeCustomize(): void {
    this.customizeOpen = false;
    if (this.customizeChanged) this.load();
  }

  exportForAccountant(): void {
    this.exporting = true;
    this.api.exportForAccountant(this.period).subscribe({
      next: blob => {
        this.exporting = false;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `accountant-export-${this.period}-${new Date().toISOString().slice(0, 10).replace(/-/g, '')}.zip`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.exporting = false;
        this.message.error('Could not build the export.');
      },
    });
  }

  // ── Cash & bank ───────────────────────────────────────────────────────────
  get notMatchedWarning(): string | null {
    const u = this.overview?.unassigned;
    if (!u || u.count === 0) return null;
    return `${u.count} customer ${u.count === 1 ? 'payment' : 'payments'} (${this.m(u.amount)}) ${u.count === 1 ? 'is' : 'are'} not in any account — ${u.methods.join(', ')} ${u.methods.length === 1 ? 'is' : 'are'} not assigned to an account.`;
  }

  accountBadge(a: FinanceAccountDto): { bg: string; fg: string } {
    return { bg: tint(a.color, 0.14), fg: a.color };
  }

  markMatched(a: FinanceAccountDto): void {
    this.api.markAccountMatched(a.id).subscribe(() => {
      this.message.success(`${a.name} marked as ${a.kind === 1 ? 'counted' : 'matched'} today.`);
      this.loadTab();
    });
  }

  daysUntil(date?: string | null): number | null {
    if (!date) return null;
    const d = new Date(date.slice(0, 10) + 'T00:00:00');
    const t = new Date();
    t.setHours(0, 0, 0, 0);
    return Math.round((d.getTime() - t.getTime()) / 86400000);
  }

  whenText(item: FinanceItemDto): string {
    const n = this.daysUntil(item.date);
    if (n == null) return 'soon';
    if (n < 0) return `${-n} ${-n === 1 ? 'day' : 'days'} late`;
    if (n === 0) return 'today';
    if (n === 1) return 'tomorrow';
    return `in ${n} days`;
  }

  receive(item: FinanceItemDto): void {
    this.openRecord({ mode: 'receive', item });
  }

  barWidth(amount: number, lines: { amount: number }[]): string {
    const max = Math.max(1, ...lines.map(l => l.amount));
    return Math.max(2, (amount / max) * 100) + '%';
  }

  get kept(): number {
    return (this.cash?.moneyIn ?? 0) - (this.cash?.moneyOut ?? 0);
  }

  deleteMovement(id: number, ev: Event): void {
    ev.stopPropagation();
    this.api.deleteTransaction(id).subscribe({ next: () => { this.message.success('Removed.'); this.load(); } });
  }

  // ── Customer dues ─────────────────────────────────────────────────────────
  agingMax(): number {
    return Math.max(1, ...(this.dues?.aging ?? []).map(a => a.amount));
  }

  agingColor(key: string): string {
    return key === 'current' ? '#0E6B3F' : key === 'b1' ? '#9A5B00' : key === 'b2' ? '#B45309' : '#B42318';
  }

  dueAction(d: FinanceDueDto): 'Remind' | 'Call' | 'View' {
    if (!d.daysLate) return 'View';
    return d.daysLate > this.setting.agingStep1 ? 'Call' : 'Remind';
  }

  reminderText(d: FinanceDueDto): string {
    const template = this.setting.reminderTemplate || 'Dear {name}, invoice {invoice} for {amount} is {days} overdue. Please arrange payment.';
    return fill(template, {
      name: d.customerName,
      invoice: d.invoiceNumber,
      amount: this.m(d.balance),
      days: `${d.daysLate ?? 0} ${d.daysLate === 1 ? 'day' : 'days'}`,
    });
  }

  runDueAction(d: FinanceDueDto): void {
    const action = this.dueAction(d);
    if (action === 'Call' && d.customerPhone) {
      window.location.href = 'tel:' + d.customerPhone;
    } else if (action === 'View') {
      this.openRecord({ mode: 'customer', invoiceId: d.invoiceId });
    } else {
      this.copy(this.reminderText(d), 'Reminder copied — paste it into SMS or WhatsApp.');
    }
  }

  remindAll(): void {
    const late = (this.dues?.dues ?? []).filter(d => d.daysLate);
    if (!late.length) {
      this.message.info('Nobody is late.');
      return;
    }
    this.copy(late.map(d => `${this.reminderText(d)}${d.customerPhone ? ' (' + d.customerPhone + ')' : ''}`).join('\n\n'), `${late.length} reminders copied.`);
  }

  private copy(text: string, ok: string): void {
    navigator.clipboard.writeText(text).then(
      () => this.message.success(ok),
      () => this.message.error('Could not copy. Allow clipboard access and try again.'),
    );
  }

  dealerPercent(d: FinanceDealerDto): number {
    return d.limit > 0 ? Math.min(100, (d.used / d.limit) * 100) : 0;
  }

  dealerColor(d: FinanceDealerDto): string {
    const p = this.dealerPercent(d);
    return p >= 85 ? '#B42318' : p >= 50 ? '#9A5B00' : '#0B5A34';
  }

  copyStatement(d: FinanceDealerDto): void {
    const lines = d.openInvoices.map(i => `${i.invoiceNumber} · due ${i.dueDate ? this.shortDate(i.dueDate) : 'on delivery'} · ${this.m(i.balance)}`);
    this.copy(`Statement for ${d.name}\n${lines.join('\n')}\nTotal owed: ${this.m(d.used)} of ${this.m(d.limit)} credit limit`, 'Statement copied.');
  }

  advanceTone(a: FinanceItemDto): string {
    return (a.extra ?? '').toLowerCase().startsWith('release') ? 'is-green' : 'is-amber';
  }

  // ── Supplier bills ────────────────────────────────────────────────────────
  billDue(b: FinanceBillDto): { text: string; cls: string } {
    if (b.status === 'paid') return { text: `Paid ${this.shortDate(b.lastPaidOn)}`, cls: 'is-green' };
    if (b.dueInDays < 0) return { text: `${-b.dueInDays} ${b.dueInDays === -1 ? 'day' : 'days'} late`, cls: 'is-red' };
    return { text: b.dueInDays === 0 ? 'today' : `in ${b.dueInDays} ${b.dueInDays === 1 ? 'day' : 'days'}`, cls: b.dueInDays <= this.setting.dueSoonDays ? 'is-amber' : '' };
  }

  billChip(b: FinanceBillDto): { text: string; cls: string } {
    switch (b.status) {
      case 'paid': return { text: 'Paid', cls: 'is-green' };
      case 'part': return { text: 'Part paid', cls: 'is-amber' };
      case 'overdue': return { text: 'Overdue', cls: 'is-red' };
      default: return { text: 'Open', cls: 'is-amber-soft' };
    }
  }

  billPaidPercent(b: FinanceBillDto): number {
    return b.amount > 0 ? Math.min(100, (b.paid / b.amount) * 100) : 0;
  }

  planMax(): number {
    return Math.max(1, ...(this.bills?.plan ?? []).flatMap(w => [w.toPay, w.expected]));
  }

  get tightSentence(): string {
    const b = this.bills;
    if (!b) return '';
    if (!b.tightWeek) return this.t('bills.planOk');
    const w = b.plan.find(x => x.label === b.tightWeek)!;
    return `${w.label} is tight: ${this.m(w.toPay)} to pay against ${this.m(w.expected)} expected. ${this.t('bills.planAdvice')}`;
  }

  lcTone(i: FinanceItemDto): { bg: string; fg: string; bar: string } {
    const c = i.color || '#9A5B00';
    return { bg: tint(c, 0.14), fg: c, bar: c };
  }

  openBill(b: FinanceBillDto | null): void {
    this.openRecord({ mode: 'bill', bill: b });
  }

  payBill(b: FinanceBillDto): void {
    this.openRecord({ mode: 'supplier', billId: b.id });
  }

  billAction(b: FinanceBillDto): void {
    if (b.status === 'paid') {
      if (b.receiptUrl) window.open(b.receiptUrl, '_blank', 'noopener');
      else this.openBill(b);
    } else {
      this.payBill(b);
    }
  }

  // ── Expenses ──────────────────────────────────────────────────────────────
  openExpense(e: FinanceExpenseDto | null): void {
    this.openRecord({ mode: 'expense', expense: e });
  }

  payRecurring(r: FinanceRecurringDto): void {
    this.openRecord({ mode: 'recurring', recurring: r });
  }

  recurringChip(r: FinanceRecurringDto): { text: string; cls: string } {
    return r.status === 'paid' ? { text: 'Paid', cls: 'is-green' } : r.status === 'due' ? { text: 'Due', cls: 'is-amber' } : { text: 'Scheduled', cls: 'is-grey' };
  }

  ordinal(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return n + (s[(v - 20) % 10] || s[v] || s[0]);
  }

  changeClass(pct: number | null | undefined): string {
    return pct == null || pct === 0 ? 'is-grey' : pct > 0 ? 'is-amber' : 'is-green';
  }

  changeArrow(pct: number | null | undefined): string {
    return pct == null ? '' : pct === 0 ? 'same' : (pct > 0 ? '▲' : '▼') + Math.abs(pct) + '%';
  }

  // ── Profit & loss ─────────────────────────────────────────────────────────
  lines(section: 'sales' | 'cost' | 'running'): FinancePnlLineDto[] {
    return (this.pnl?.lines ?? []).filter(l => l.section === section);
  }

  lineLabel(l: FinancePnlLineDto): string {
    const key: Record<string, string> = { gross: 'pl.sales', vat: 'pl.vat', net: 'pl.net', grossProfit: 'pl.gross', profit: 'pl.profit' };
    return key[l.key] ? this.t(key[l.key]) : l.label;
  }

  /** Costs rising is bad (red); sales and profit rising is good (green). */
  changeTone(l: FinancePnlLineDto): string {
    if (l.changePercent == null || l.changePercent === 0) return 'is-grey';
    const costLine = l.section !== 'sales' && !l.isTotal;
    return (l.changePercent > 0) === costLine ? 'is-red' : 'is-green';
  }

  barHeight(b: FinanceBarDto): string {
    const max = Math.max(1, ...(this.pnl?.bars ?? []).map(x => Math.abs(x.profit)));
    return Math.max(3, (Math.abs(b.profit) / max) * 100) + '%';
  }

  insightTitle(i: FinanceInsightDto): string {
    return this.t(i.kind === 'best' ? 'pl.bestTitle' : 'pl.dipTitle', { month: i.label });
  }

  insightText(i: FinanceInsightDto): string {
    return this.t(i.kind === 'best' ? 'pl.bestText' : 'pl.dipText', { amount: this.k(i.amount), pct: Math.abs(i.percentVsAverage ?? 0) });
  }

  noteColor(n: FinanceItemDto): string {
    return n.color || '#6B7280';
  }
}

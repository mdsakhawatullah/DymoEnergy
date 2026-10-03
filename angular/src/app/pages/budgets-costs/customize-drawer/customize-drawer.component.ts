import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { FinanceService } from '../../../proxy/finance/finance.service';
import {
  FinanceCostGroup,
  FinanceItemDto,
  FinanceItemKind,
  FinanceOverviewDto,
} from '../../../proxy/finance/models';
import { ColorFieldComponent } from '../../project-planning/customize-drawer/color-field.component';
import { parseDateKey, toDateKey } from '../../project-planning/project-planning.utils';
import { PAYMENT_METHODS } from '../finance.utils';

export type FinanceCustomizeTab = 'page' | 'accounts' | 'categories' | 'recurring' | 'transit' | 'advances' | 'lcs' | 'dealers' | 'notes';

type FieldType = 'text' | 'textarea' | 'money' | 'int' | 'date' | 'color' | 'switch' | 'select';

interface Field {
  key: string;
  label: string;
  type: FieldType;
  placeholder?: string;
  options?: (o: FinanceOverviewDto) => { value: number | null; label: string }[];
  max?: number;
}

interface Row { id: number; data: any; open: boolean; saving: boolean; }

interface ListConfig {
  tab: Exclude<FinanceCustomizeTab, 'page'>;
  label: string;
  help: string;
  newTitle: string;
  titleKey: string;
  fields: Field[];
  defaults: any;
  /** Raw server rows for this list. */
  source: (o: FinanceOverviewDto) => any[];
  toBody: (data: any, order: number) => any;
  create: (api: FinanceService, body: any) => Observable<any>;
  update: (api: FinanceService, id: number, body: any) => Observable<any>;
  remove: (api: FinanceService, id: number) => Observable<any>;
  /** Short grey text shown next to the title. */
  summary?: (data: any) => string;
}

const dateOf = (v?: string | null) => (v ? parseDateKey(v.slice(0, 10)) : null);
const keyOf = (d: Date | null) => (d ? toDateKey(d) : null);
const trimOrNull = (s?: string | null) => (s?.trim() ? s.trim() : null);

function itemConfig(
  tab: ListConfig['tab'], label: string, kind: FinanceItemKind, help: string, newTitle: string, fields: Field[], defaults: any = {}, summary?: ListConfig['summary'],
): ListConfig {
  return {
    tab, label, help, newTitle, titleKey: 'title', fields, summary,
    defaults: { title: '', detail: '', extra: '', color: '#2563EB', amount: 0, date: null, percent: null, ...defaults },
    source: o => o.items.filter(i => i.kind === kind).sort((a, b) => a.order - b.order),
    toBody: (d, order) => ({
      kind, title: d.title, detail: trimOrNull(d.detail), extra: trimOrNull(d.extra), color: d.color ?? null, amount: d.amount ?? 0,
      date: keyOf(d.date), percent: d.percent ?? null, flag: false, order,
    }),
    create: (api, body) => api.createItem(body),
    update: (api, id, body) => api.updateItem(id, body),
    remove: (api, id) => api.deleteItem(id),
  };
}

const LISTS: ListConfig[] = [
  {
    tab: 'accounts', label: 'Accounts', newTitle: 'New account', titleKey: 'name',
    help: 'Where your money sits: cash, mobile wallets and bank accounts. Customer payments land in the account that claims their payment method, and a method can belong to only one account.',
    defaults: { name: '', kind: 3, shortCode: '', color: '#2563EB', openingBalance: 0, openingDate: new Date(), methods: '', isActive: true },
    fields: [
      { key: 'name', label: 'Name', type: 'text' },
      { key: 'kind', label: 'Kind', type: 'select', options: () => [{ value: 1, label: 'Cash' }, { value: 2, label: 'Mobile wallet' }, { value: 3, label: 'Bank' }] },
      { key: 'shortCode', label: 'Short code on the card (e.g. BNK)', type: 'text', max: 8 },
      { key: 'color', label: 'Colour', type: 'color' },
      { key: 'openingBalance', label: 'Balance on the opening date', type: 'money' },
      { key: 'openingDate', label: 'Opening date (money before this day is already in the balance)', type: 'date' },
      { key: 'methods', label: 'Customer payment methods that land here (comma separated)', type: 'text', placeholder: PAYMENT_METHODS.map(m => m.name).join(', ') },
      { key: 'isActive', label: 'In use', type: 'switch' },
    ],
    source: o => o.allAccounts,
    summary: d => d.shortCode,
    toBody: (d, order) => ({
      name: d.name, kind: d.kind, shortCode: d.shortCode, color: d.color, openingBalance: d.openingBalance ?? 0, openingDate: keyOf(d.openingDate),
      paymentMethods: String(d.methods ?? '').split(',').map((x: string) => x.trim()).filter(Boolean), isActive: d.isActive, order,
    }),
    create: (api, b) => api.createAccount(b), update: (api, id, b) => api.updateAccount(id, b), remove: (api, id) => api.deleteAccount(id),
  },
  {
    tab: 'categories', label: 'Cost categories', newTitle: 'New category', titleKey: 'name',
    help: 'What money is spent on. “Cost of what you sold” categories come off sales before gross profit; “Running costs” come off after. Categories that share a statement line are added together on the Profit & loss tab.',
    defaults: { name: '', color: '#6B7280', costGroup: 2, plLine: '', isActive: true },
    fields: [
      { key: 'name', label: 'Name', type: 'text' },
      { key: 'color', label: 'Colour', type: 'color' },
      { key: 'costGroup', label: 'Counts as', type: 'select', options: () => [{ value: FinanceCostGroup.CostOfSales, label: 'Cost of what you sold' }, { value: FinanceCostGroup.Running, label: 'Running cost' }] },
      { key: 'plLine', label: 'Profit & loss line (leave empty to use the name)', type: 'text' },
      { key: 'isActive', label: 'In use', type: 'switch' },
    ],
    source: o => o.allCategories,
    summary: d => (d.costGroup === FinanceCostGroup.CostOfSales ? 'cost of sales' : 'running'),
    toBody: (d, order) => ({ name: d.name, color: d.color, costGroup: d.costGroup, plLine: trimOrNull(d.plLine), isActive: d.isActive, order }),
    create: (api, b) => api.createCategory(b), update: (api, id, b) => api.updateCategory(id, b), remove: (api, id) => api.deleteCategory(id),
  },
  {
    tab: 'recurring', label: 'Monthly costs', newTitle: 'New monthly cost', titleKey: 'name',
    help: 'Costs that come back every month (salaries, rent). On the Expenses tab you record each month’s payment with one click.',
    defaults: { name: '', detail: '', dayOfMonth: 1, amount: 0, categoryId: null, accountId: null, isActive: true },
    fields: [
      { key: 'name', label: 'Name', type: 'text' },
      { key: 'detail', label: 'Note (default: “Paid on the 5th”)', type: 'text' },
      { key: 'dayOfMonth', label: 'Day of the month', type: 'int' },
      { key: 'amount', label: 'Usual amount', type: 'money' },
      { key: 'categoryId', label: 'Category', type: 'select', options: o => o.categories.map(c => ({ value: c.id, label: c.name })) },
      { key: 'accountId', label: 'Usually paid from', type: 'select', options: o => [{ value: null, label: 'No account' }, ...o.accounts.map(a => ({ value: a.id, label: a.name }))] },
      { key: 'isActive', label: 'In use', type: 'switch' },
    ],
    source: o => o.recurring,
    summary: d => `day ${d.dayOfMonth}`,
    toBody: (d, order) => ({ name: d.name, detail: trimOrNull(d.detail), dayOfMonth: d.dayOfMonth ?? 1, amount: d.amount ?? 0, categoryId: d.categoryId, accountId: d.accountId ?? null, isActive: d.isActive, order }),
    create: (api, b) => api.createRecurring(b), update: (api, id, b) => api.updateRecurring(id, b), remove: (api, id) => api.deleteRecurring(id),
  },
  itemConfig('transit', 'Money on the way', FinanceItemKind.InTransit,
    'Money that is already yours but has not reached an account yet: card settlements, courier cash-on-delivery, cheques. Click its date chip on the page when it arrives.',
    'New item',
    [
      { key: 'title', label: 'What', type: 'text' }, { key: 'detail', label: 'Detail', type: 'text' },
      { key: 'amount', label: 'Amount', type: 'money' }, { key: 'date', label: 'Expected on', type: 'date' },
      { key: 'extra', label: 'Badge text (e.g. CARD, COD, CHQ)', type: 'text', max: 6 }, { key: 'color', label: 'Badge colour', type: 'color' },
    ]),
  itemConfig('advances', 'Advances held', FinanceItemKind.Advance,
    'Money taken from a customer before the job is done. It is shown as held until the job is handed over.',
    'New advance',
    [
      { key: 'title', label: 'Customer · job', type: 'text' }, { key: 'detail', label: 'Where the job is', type: 'text' },
      { key: 'amount', label: 'Amount held', type: 'money' }, { key: 'extra', label: 'Status (Held / Releasing)', type: 'text' },
    ], { extra: 'Held' }),
  itemConfig('lcs', 'Import payments (LC)', FinanceItemKind.LetterOfCredit,
    'Letters of credit opened with the bank, tied to the shipment they pay for.',
    'New letter of credit',
    [
      { key: 'title', label: 'LC number · goods', type: 'text' }, { key: 'detail', label: 'Supplier and where it stands', type: 'text' },
      { key: 'amount', label: 'Amount', type: 'money' }, { key: 'extra', label: 'Status label (e.g. Margin paid)', type: 'text' },
      { key: 'percent', label: 'Paid so far (%)', type: 'int' }, { key: 'color', label: 'Status colour', type: 'color' },
    ], { color: '#9A5B00', percent: 0 }),
  itemConfig('dealers', 'Dealer credit', FinanceItemKind.DealerCredit,
    'Installers who buy on account. The amount used is worked out from their unpaid invoices, so the name must match the customer name on their invoices exactly.',
    'New dealer',
    [
      { key: 'title', label: 'Dealer (exactly as on invoices)', type: 'text' }, { key: 'detail', label: 'Terms (e.g. 30 days · list B)', type: 'text' },
      { key: 'amount', label: 'Credit limit', type: 'money' },
    ]),
  itemConfig('notes', 'Notes', FinanceItemKind.Insight,
    'Your own “worth knowing” notes, shown under the profit chart next to the automatic insights.',
    'New note',
    [
      { key: 'title', label: 'Headline', type: 'text' }, { key: 'detail', label: 'Detail', type: 'textarea' }, { key: 'color', label: 'Dot colour', type: 'color' },
    ], { color: '#0E6B3F' }),
];

@Component({
  selector: 'finance-customize-drawer',
  templateUrl: './customize-drawer.component.html',
  styleUrl: './customize-drawer.component.css',
  imports: [SharedModule, ColorFieldComponent],
})
export class FinanceCustomizeDrawerComponent implements OnInit {
  @Input({ required: true }) overview!: FinanceOverviewDto;
  @Input() initialTab: FinanceCustomizeTab = 'page';

  @Output() changed = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  readonly lists = LISTS;
  tabIndex = 0;

  // page tab
  accent = '#0E6B3F';
  symbol = '৳';
  compact = true;
  dueSoon = 7;
  step1 = 30;
  step2 = 60;
  chartMonths = 6;
  reminder = '';
  labelValues: Record<string, string> = {};
  groups: { name: string; labels: { key: string; caption: string }[] }[] = [];
  savingSetting = false;

  rows: Record<string, Row[]> = {};

  constructor(
    private api: FinanceService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    const s = this.overview.setting;
    this.accent = s.accentColor; this.symbol = s.currencySymbol; this.compact = s.compactMoney; this.dueSoon = s.dueSoonDays;
    this.step1 = s.agingStep1; this.step2 = s.agingStep2; this.chartMonths = s.chartMonths; this.reminder = s.reminderTemplate ?? '';
    for (const l of s.labels) {
      this.labelValues[l.key] = l.value;
      let g = this.groups.find(x => x.name === l.group);
      if (!g) this.groups.push((g = { name: l.group, labels: [] }));
      g.labels.push({ key: l.key, caption: l.caption });
    }
    for (const cfg of LISTS) this.rows[cfg.tab] = cfg.source(this.overview).map(r => this.toRow(cfg, r));
    this.tabIndex = this.initialTab === 'page' ? 0 : 1 + LISTS.findIndex(c => c.tab === this.initialTab);
  }

  /** Server row → editable row (dates become Date objects, account methods become one text box). */
  private toRow(cfg: ListConfig, r: any): Row {
    const data: any = { ...cfg.defaults };
    for (const f of cfg.fields) if (f.key in r) data[f.key] = r[f.key] ?? cfg.defaults[f.key] ?? null;
    if (cfg.tab === 'accounts') { data.methods = (r.paymentMethods ?? []).join(', '); data.openingDate = dateOf(r.openingDate); }
    if (cfg.tab === 'transit') data.date = dateOf(r.date);
    return { id: r.id, open: false, saving: false, data: { ...data, order: r.order } };
  }

  // ── Page tab ──────────────────────────────────────────────────────────────
  saveSetting(): void {
    this.savingSetting = true;
    this.api.updateSetting({
      accentColor: this.accent, currencySymbol: this.symbol, compactMoney: this.compact, dueSoonDays: this.dueSoon, agingStep1: this.step1,
      agingStep2: this.step2, chartMonths: this.chartMonths, reminderTemplate: this.reminder || null, labels: this.labelValues,
    }).subscribe({
      next: () => { this.savingSetting = false; this.message.success('Page settings saved.'); this.changed.emit(); },
      error: () => (this.savingSetting = false),
    });
  }

  // ── List tabs ─────────────────────────────────────────────────────────────
  options(f: Field) {
    return f.options ? f.options(this.overview) : [];
  }

  titleOf(cfg: ListConfig, r: Row): string {
    return r.data[cfg.titleKey] || cfg.newTitle;
  }

  add(cfg: ListConfig): void {
    const list = this.rows[cfg.tab];
    const data = { ...cfg.defaults };
    if (cfg.tab === 'recurring') data.categoryId = this.overview.categories[0]?.id ?? null;
    list.push({ id: 0, open: true, saving: false, data: { ...data, order: Math.max(0, ...list.map(r => r.data.order ?? 0)) + 1 } });
  }

  save(cfg: ListConfig, r: Row): void {
    if (!String(r.data[cfg.titleKey] ?? '').trim()) return void this.message.warning('Fill in the name first.');
    r.saving = true;
    const body = cfg.toBody(r.data, r.data.order ?? 0);
    (r.id ? cfg.update(this.api, r.id, body) : cfg.create(this.api, body)).subscribe({
      next: saved => {
        r.id = saved.id; r.saving = false; r.open = false;
        this.message.success('Saved.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  remove(cfg: ListConfig, r: Row): void {
    if (!r.id) { this.rows[cfg.tab] = this.rows[cfg.tab].filter(x => x !== r); return; }
    r.saving = true;
    cfg.remove(this.api, r.id).subscribe({
      next: () => { this.rows[cfg.tab] = this.rows[cfg.tab].filter(x => x !== r); this.message.success('Deleted.'); this.changed.emit(); },
      error: () => (r.saving = false),
    });
  }

  move(cfg: ListConfig, index: number, dir: -1 | 1): void {
    const list = this.rows[cfg.tab];
    const a = list[index]; const b = list[index + dir];
    if (!a || !b) return;
    [a.data.order, b.data.order] = [b.data.order, a.data.order];
    if (a.data.order === b.data.order) a.data.order += dir;
    [list[index], list[index + dir]] = [b, a];
    const calls = [a, b].filter(r => r.id).map(r => cfg.update(this.api, r.id, cfg.toBody(r.data, r.data.order)));
    if (calls.length) forkJoin(calls).subscribe({ next: () => this.changed.emit(), error: () => this.message.error('Could not reorder.') });
  }
}

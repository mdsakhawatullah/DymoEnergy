import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PermissionService } from '@abp/ng.core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { SharedModule } from '../../shared/shared.module';
import { StockService } from '../../proxy/stock/stock.service';
import {
  StockEntriesPageDto,
  StockEntryDto,
  StockEntryStatus,
  StockEntrySummaryDto,
  StockEntryType,
  StockOverviewDto,
} from '../../proxy/stock/models';
import { TYPES, isoDate, lakh, money, printSlip, signed, statusLabel, typeMeta } from './stock.utils';
import { StockPlacesComponent } from './places/stock-places.component';

type TabKey = 'all' | 'in' | 'out' | 'transfer' | 'adjustment' | 'drafts';

const PERM = { edit: 'DymoEnergy.Stock.Edit', post: 'DymoEnergy.Stock.Post' };
const PAGE_SIZE = 8;

@Component({
  selector: 'app-stock-entries',
  templateUrl: './stock-entries.component.html',
  styleUrls: ['./stock.css', './stock-entries.component.css'],
  imports: [SharedModule, StockPlacesComponent],
})
export class StockEntriesComponent implements OnInit, OnDestroy {
  readonly Type = StockEntryType;
  readonly Status = StockEntryStatus;
  readonly types = TYPES;
  money = money;
  lakh = lakh;
  signed = signed;
  typeMeta = typeMeta;
  statusLabel = statusLabel;

  overview: StockOverviewDto | null = null;
  page: StockEntriesPageDto | null = null;
  loading = false;
  failed = false;

  tab: TabKey = 'all';
  filter = '';
  warehouseId: number | null = null;
  days: number | null = 30;
  status: StockEntryStatus | null = null;
  pageIndex = 1;

  expanded = new Set<number>();
  details: Record<number, StockEntryDto> = {};
  busy = new Set<number>();
  highlight: number | null = null;
  placesOpen = false;

  readonly tabs: { key: TabKey; label: string; dot?: string; count: (c: StockEntriesPageDto['counts']) => number }[] = [
    { key: 'all', label: 'All', count: c => c.all },
    { key: 'in', label: 'Stock in', dot: '#0E6B3F', count: c => c.stockIn },
    { key: 'out', label: 'Stock out', dot: '#B42318', count: c => c.stockOut },
    { key: 'transfer', label: 'Transfer', dot: '#2563EB', count: c => c.transfer },
    { key: 'adjustment', label: 'Adjustment', dot: '#D97706', count: c => c.adjustment },
    { key: 'drafts', label: 'Drafts', dot: '#9AA39C', count: c => c.drafts },
  ];

  readonly ranges = [
    { value: 7, label: 'Last 7 days' },
    { value: 30, label: 'Last 30 days' },
    { value: 0, label: 'This month' },
    { value: 90, label: 'Last 90 days' },
    { value: 365, label: 'Last 12 months' },
    { value: null, label: 'All time' },
  ];

  canEdit = false;
  canPost = false;

  private search$ = new Subject<void>();
  private destroy$ = new Subject<void>();

  constructor(
    private api: StockService,
    private message: NzMessageService,
    private router: Router,
    route: ActivatedRoute,
    permissions: PermissionService,
  ) {
    this.canEdit = permissions.getGrantedPolicy(PERM.edit);
    this.canPost = permissions.getGrantedPolicy(PERM.post);
    const h = Number(route.snapshot.queryParamMap.get('highlight'));
    if (h) { this.highlight = h; this.expanded.add(h); }
  }

  ngOnInit(): void {
    this.loadOverview();
    this.load();
    this.search$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => { this.pageIndex = 1; this.load(); });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Loading ─────────────────────────────────────────────────────────────
  loadOverview(): void {
    this.api.getOverview().subscribe(o => (this.overview = o));
  }

  private query(skip: number, take: number) {
    const type = { in: StockEntryType.StockIn, out: StockEntryType.StockOut, transfer: StockEntryType.Transfer, adjustment: StockEntryType.Adjustment }[this.tab as string];
    return {
      filter: this.filter.trim() || undefined, type, drafts: this.tab === 'drafts' || undefined, status: this.status ?? undefined,
      warehouseId: this.warehouseId ?? undefined, days: this.days ?? undefined, skipCount: skip, maxResultCount: take,
    };
  }

  load(): void {
    this.loading = true;
    this.failed = false;
    this.api.getEntries(this.query((this.pageIndex - 1) * PAGE_SIZE, PAGE_SIZE)).subscribe({
      next: p => {
        this.page = p;
        this.loading = false;
        for (const id of this.expanded) if (p.items.some(i => i.id === id) && !this.details[id]) this.loadDetail(id);
      },
      error: () => { this.loading = false; this.failed = true; },
    });
  }

  onSearch(): void {
    this.search$.next();
  }

  setTab(t: TabKey): void {
    this.tab = t;
    this.pageIndex = 1;
    this.load();
  }

  filtersChanged(): void {
    this.pageIndex = 1;
    this.load();
  }

  goTo(p: number): void {
    this.pageIndex = p;
    this.load();
  }

  get pageCount(): number {
    return Math.max(1, Math.ceil((this.page?.totalCount ?? 0) / PAGE_SIZE));
  }

  /** 1 2 3 … 18 style page list. */
  get pages(): (number | null)[] {
    const n = this.pageCount, c = this.pageIndex;
    if (n <= 7) return Array.from({ length: n }, (_, i) => i + 1);
    const set = new Set([1, 2, n, c - 1, c, c + 1].filter(p => p >= 1 && p <= n));
    if (c <= 3) [3, 4].forEach(p => set.add(p));
    const sorted = [...set].sort((a, b) => a - b);
    const out: (number | null)[] = [];
    sorted.forEach((p, i) => { if (i && p - sorted[i - 1] > 1) out.push(null); out.push(p); });
    return out;
  }

  // ── Rows ────────────────────────────────────────────────────────────────
  toggle(e: StockEntrySummaryDto): void {
    if (this.expanded.has(e.id)) { this.expanded.delete(e.id); return; }
    this.expanded.add(e.id);
    if (!this.details[e.id]) this.loadDetail(e.id);
  }

  private loadDetail(id: number): void {
    this.api.get(id).subscribe(d => (this.details[id] = d));
  }

  unitsClass(e: StockEntrySummaryDto): string {
    if (e.type === StockEntryType.Transfer) return 'is-blue';
    return e.units > 0 ? 'is-green' : e.units < 0 ? 'is-red' : '';
  }

  unitsText(e: StockEntrySummaryDto): string {
    return e.type === StockEntryType.Transfer ? String(e.units) : signed(e.units);
  }

  statusClass(s: StockEntryStatus): string {
    return s === StockEntryStatus.Draft ? 'is-draft' : s === StockEntryStatus.Reversed ? 'is-reversed' : 'is-posted';
  }

  lineQty(d: StockEntryDto, change: number, qty: number): string {
    if (d.type === StockEntryType.Transfer) return String(qty);
    if (d.status === StockEntryStatus.Draft) {
      return d.type === StockEntryType.StockOut ? `−${qty}` : d.type === StockEntryType.StockIn ? `+${qty}` : '—';
    }
    return signed(change);
  }

  open(e: StockEntrySummaryDto): void {
    this.router.navigate(['/stock-entries', e.id]);
  }

  print(d: StockEntryDto): void {
    printSlip(d);
  }

  post(e: StockEntrySummaryDto): void {
    this.busy.add(e.id);
    this.api.post(e.id).subscribe({
      next: d => {
        this.busy.delete(e.id);
        this.details[e.id] = d;
        this.message.success(`${d.number} posted. Stock is updated.`);
        this.refresh();
      },
      error: () => this.busy.delete(e.id),
    });
  }

  reverse(e: StockEntrySummaryDto): void {
    this.busy.add(e.id);
    this.api.reverse(e.id).subscribe({
      next: r => {
        this.busy.delete(e.id);
        delete this.details[e.id];
        this.message.success(`${e.number} undone by ${r.number}.`);
        this.highlight = r.id;
        this.refresh();
      },
      error: () => this.busy.delete(e.id),
    });
  }

  deleteDraft(e: StockEntrySummaryDto): void {
    this.api.delete(e.id).subscribe(() => {
      this.expanded.delete(e.id);
      this.message.success(`Draft ${e.number} deleted.`);
      this.refresh();
    });
  }

  refresh(): void {
    this.load();
    this.loadOverview();
  }

  // ── Header actions ──────────────────────────────────────────────────────
  receiveRestock(): void {
    const ids = (this.overview?.restock ?? []).map(r => r.productId);
    this.router.navigate(['/stock-entries/new'], { queryParams: { type: 'in', products: ids.join(',') } });
  }

  receiveOne(productId: number): void {
    this.router.navigate(['/stock-entries/new'], { queryParams: { type: 'in', products: productId } });
  }

  get outText(): string {
    const parts = this.overview?.outParts ?? [];
    return parts.length ? parts.map(p => `${p.units} ${p.label}`).join(' · ') : 'nothing went out yet';
  }

  exporting = false;

  export(): void {
    this.exporting = true;
    this.api.getEntries(this.query(0, 1000)).subscribe({
      next: p => {
        this.exporting = false;
        const head = ['Entry', 'Date', 'Type', 'Status', 'Supplier / reason', 'Details', 'Items', 'Units', 'Value'];
        const rows = p.items.map(e => [
          e.number, isoDate(new Date(e.date)), typeMeta(e.type).label, statusLabel(e.status), e.title, e.subtitle, e.itemCount, e.units, e.value,
        ]);
        const csv = [head, ...rows].map(r => r.map(v => `"${String(v ?? '').replace(/"/g, '""')}"`).join(',')).join('\r\n');
        const url = URL.createObjectURL(new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8' }));
        const a = document.createElement('a');
        a.href = url;
        a.download = `stock-entries-${isoDate(new Date())}.csv`;
        a.click();
        URL.revokeObjectURL(url);
        if (p.totalCount > p.items.length) this.message.info(`Exported the newest ${p.items.length} of ${p.totalCount} entries.`);
      },
      error: () => (this.exporting = false),
    });
  }
}

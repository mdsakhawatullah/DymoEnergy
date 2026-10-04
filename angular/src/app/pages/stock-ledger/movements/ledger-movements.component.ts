import { Component, EventEmitter, OnDestroy, OnInit, Output } from '@angular/core';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { SharedModule } from '../../../shared/shared.module';
import { StockLedgerService } from '../../../proxy/stock/stock-ledger.service';
import { StockService } from '../../../proxy/stock/stock.service';
import { WarehouseDto } from '../../../proxy/stock/models';
import { LedgerLineDto, LedgerLinesPageDto, LedgerMovement, MOVEMENTS } from '../../../proxy/stock/ledger.models';
import { flagTone, movementTone, signed } from '../ledger.utils';

const PAGE_SIZE = 12;

@Component({
  selector: 'app-ledger-movements',
  templateUrl: './ledger-movements.component.html',
  styleUrls: ['../ledger-shared.css', './ledger-movements.component.css'],
  imports: [SharedModule],
})
export class LedgerMovementsComponent implements OnInit, OnDestroy {
  @Output() openLine = new EventEmitter<number>();

  readonly movements = MOVEMENTS;
  signed = signed;
  movementTone = movementTone;
  flagTone = flagTone;

  page: LedgerLinesPageDto | null = null;
  warehouses: WarehouseDto[] = [];
  loading = false;
  failed = false;

  filter = '';
  movement: LedgerMovement | null = null;
  warehouseId: number | null = null;
  days: number | null = 30;
  flaggedOnly = false;
  pageIndex = 1;

  readonly ranges = [
    { value: 7, label: 'Last 7 days' },
    { value: 30, label: 'Last 30 days' },
    { value: 90, label: 'Last 90 days' },
    { value: 365, label: 'Last 12 months' },
    { value: null, label: 'Everything that happened' },
  ];

  private search$ = new Subject<void>();
  private destroy$ = new Subject<void>();

  constructor(private api: StockLedgerService, private stock: StockService) {}

  ngOnInit(): void {
    this.load();
    this.stock.getOverview().subscribe(o => (this.warehouses = o.warehouses));
    this.search$.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => { this.pageIndex = 1; this.load(); });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(): void {
    this.loading = true;
    this.failed = false;
    this.api.getLines({
      filter: this.filter.trim() || undefined, movement: this.movement ?? undefined,
      warehouseId: this.warehouseId ?? undefined, days: this.days ?? undefined,
      flaggedOnly: this.flaggedOnly || undefined, skipCount: (this.pageIndex - 1) * PAGE_SIZE, maxResultCount: PAGE_SIZE,
    }).subscribe({
      next: p => { this.page = p; this.loading = false; },
      error: () => { this.loading = false; this.failed = true; },
    });
  }

  onSearch(): void {
    this.search$.next();
  }

  changed(): void {
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

  /** The quantity after the move, with the one before it for context. */
  trail(l: LedgerLineDto): string {
    return `${l.quantityBefore} → ${l.quantityAfter}`;
  }
}

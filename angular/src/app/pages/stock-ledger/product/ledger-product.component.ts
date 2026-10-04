import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { StockLedgerService } from '../../../proxy/stock/stock-ledger.service';
import { LedgerBalancePointDto, LedgerProductDto, LedgerProductOptionDto } from '../../../proxy/stock/ledger.models';
import { lakh, money, movementTone, signed } from '../ledger.utils';

/** One point of the drawn line, in the chart's own coordinates. */
interface Plot { x: number; y: number; point: LedgerBalancePointDto; }

const W = 720;
const H = 150;
const PAD = 8;

@Component({
  selector: 'app-ledger-product',
  templateUrl: './ledger-product.component.html',
  styleUrls: ['../ledger-shared.css', './ledger-product.component.css'],
  imports: [SharedModule],
})
export class LedgerProductComponent implements OnInit {
  @Output() openLine = new EventEmitter<number>();

  readonly width = W;
  readonly height = H;
  money = money;
  lakh = lakh;
  signed = signed;
  movementTone = movementTone;

  options: LedgerProductOptionDto[] = [];
  productId: number | null = null;
  days = 30;
  data: LedgerProductDto | null = null;
  loading = false;

  readonly ranges = [
    { value: 30, label: 'Last 30 days' },
    { value: 90, label: 'Last 90 days' },
    { value: 365, label: 'Last 12 months' },
  ];

  constructor(private api: StockLedgerService) {}

  ngOnInit(): void {
    this.api.getProductOptions().subscribe(list => {
      this.options = list;
      if (list.length) { this.productId = list[0].id; this.load(); }
    });
  }

  load(): void {
    if (!this.productId) return;
    this.loading = true;
    this.api.getProduct(this.productId, this.days).subscribe({
      next: d => { this.data = d; this.loading = false; },
      error: () => (this.loading = false),
    });
  }

  // ── Chart ───────────────────────────────────────────────────────────────

  get plots(): Plot[] {
    const points = this.data?.balance ?? [];
    if (points.length === 0) return [];
    const top = Math.max(1, ...points.map(p => p.quantity));
    const step = points.length > 1 ? (W - PAD * 2) / (points.length - 1) : 0;
    return points.map((point, i) => ({
      x: PAD + i * step,
      y: PAD + (H - PAD * 2) * (1 - point.quantity / top),
      point,
    }));
  }

  get linePath(): string {
    return this.plots.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x.toFixed(1)},${p.y.toFixed(1)}`).join(' ');
  }

  get areaPath(): string {
    const p = this.plots;
    if (p.length === 0) return '';
    return `${this.linePath} L${p[p.length - 1].x.toFixed(1)},${H - PAD} L${p[0].x.toFixed(1)},${H - PAD} Z`;
  }

  /** Only the days something actually moved get a dot. */
  get dots(): Plot[] {
    return this.plots.filter(p => p.point.in > 0 || p.point.out > 0);
  }

  dotColour(p: Plot): string {
    return p.point.in > 0 && p.point.out > 0 ? '#2563EB' : p.point.in > 0 ? '#0E6B3F' : '#D97706';
  }

  dotTitle(p: Plot): string {
    const bits: string[] = [];
    if (p.point.in) bits.push(`+${p.point.in} in`);
    if (p.point.out) bits.push(`−${p.point.out} out`);
    return `${new Date(p.point.date).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })}: ${bits.join(', ')} · ${p.point.quantity} left`;
  }

  get lowestText(): string {
    const points = this.data?.balance ?? [];
    if (points.length === 0) return '';
    const low = points.reduce((a, b) => (b.quantity < a.quantity ? b : a));
    return `The lowest it reached was ${low.quantity} on ${new Date(low.date).toLocaleDateString('en-GB', { day: 'numeric', month: 'long' })}.`;
  }
}

import { Component, Input } from '@angular/core';
import { NamedValueDto } from '../../proxy/analytics/models';
import { formatTaka, formatTakaCompact } from '../sales-invoices/invoice-format';

export type ValueFormat = 'money' | 'number' | 'percent';

export function fmtValue(v: number | null | undefined, format: ValueFormat = 'money', compact = true): string {
  if (v == null) return '—';
  if (format === 'money')   return compact ? formatTakaCompact(v) : formatTaka(v);
  if (format === 'percent') return `${Math.round(v * 100)}%`;
  return new Intl.NumberFormat('en-IN', { maximumFractionDigits: 1 }).format(v);
}

/**
 * Fixed-order categorical hues (validated: CVD ΔE ≥ 12, normal ≥ 25 on the light surface).
 * Colour follows the entity's position in the server's list, never re-cycled.
 */
export const CATEGORICAL = ['#0E6B3F', '#2F6FE0', '#D98A0B', '#8B5CF6', '#C2410C'];

/** ▲ 14% / ▼ 3%. `invert` when an increase is bad (money due, refunds). */
@Component({
  selector: 'an-change',
  template: `
    @if (value != null) {
      <span class="an-change" [class.an-change--good]="good" [class.an-change--bad]="!good && value !== 0">
        <span aria-hidden="true">{{ value >= 0 ? '▲' : '▼' }}</span>
        {{ abs }}{{ suffix }}
        <span class="an-sr">{{ value >= 0 ? 'up' : 'down' }}</span>
      </span>
    }
  `,
  styles: [`
    .an-change { font-size: 12px; font-weight: 600; white-space: nowrap; color: var(--de-text-muted); }
    .an-change--good { color: var(--de-success-fg); }
    .an-change--bad  { color: var(--de-danger-fg); }
    .an-sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
  `],
})
export class AnChangeComponent {
  @Input() value: number | null | undefined = null;
  @Input() invert = false;
  @Input() suffix = '%';

  get abs(): string { return Math.abs(Math.round((this.value ?? 0) * 10) / 10).toString(); }
  get good(): boolean { return this.invert ? (this.value ?? 0) <= 0 : (this.value ?? 0) >= 0; }
}

/**
 * Horizontal bar list: name · bar · value · (share | change | count).
 * Bars are scaled to the largest value in the list and share one hue (magnitude, not identity).
 */
@Component({
  selector: 'an-bar-list',
  imports: [AnChangeComponent],
  template: `
    <ul class="bl" [style.--bl-name]="nameWidth">
      @for (it of items; track it.name; let i = $index) {
        <li class="bl__row" [title]="it.name + ': ' + fmt(it.value, false) + (it.count != null ? ' · ' + it.count : '')">
          <span class="bl__name">{{ it.name }}</span>
          <span class="bl__track">
            <span class="bl__fill" [style.width.%]="pct(it.value)" [style.background]="colorFor(i)"></span>
          </span>
          <span class="bl__value">{{ fmt(it.value) }}</span>
          @switch (right) {
            @case ('share')  { <span class="bl__aside">{{ round(it.share * 100) }}%</span> }
            @case ('change') { <span class="bl__aside"><an-change [value]="it.change" [invert]="invertChange"></an-change></span> }
            @case ('count')  { <span class="bl__aside">{{ it.count ?? '' }}{{ countSuffix }}</span> }
          }
        </li>
      } @empty {
        <li class="bl__empty">{{ empty }}</li>
      }
    </ul>
  `,
  styles: [`
    .bl { list-style: none; margin: 0; padding: 0; }
    .bl__row {
      display: grid;
      grid-template-columns: var(--bl-name, minmax(110px, 1.1fr)) minmax(60px, 1.4fr) auto 52px;
      align-items: center; gap: 12px;
      padding: 7px 0; font-size: 13.5px;
      border-bottom: 1px solid var(--de-border-soft);
    }
    .bl__row:last-child { border-bottom: none; }
    .bl__name  { font-weight: 500; color: var(--de-text); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .bl__track { height: 6px; border-radius: 999px; background: #ECEFE9; overflow: hidden; }
    .bl__fill  { display: block; height: 100%; border-radius: 999px; min-width: 3px; }
    .bl__value { font-weight: 600; color: var(--de-text); text-align: right; white-space: nowrap; font-variant-numeric: tabular-nums; }
    .bl__aside { font-size: 12px; color: var(--de-text-muted); text-align: right; white-space: nowrap; }
    .bl__empty { padding: 18px 0; text-align: center; color: var(--de-placeholder); font-size: 13px; }
  `],
})
export class AnBarListComponent {
  @Input() items: NamedValueDto[] = [];
  @Input() format: ValueFormat = 'money';
  @Input() right: 'share' | 'change' | 'count' | 'none' = 'share';
  @Input() invertChange = false;
  @Input() countSuffix = '';
  @Input() nameWidth?: string;
  /** Single hue by default; pass per-row colours only when a bar encodes status. */
  @Input() colors?: string[];
  @Input() empty = 'Nothing in this period.';

  private get max(): number { return Math.max(1, ...this.items.map(i => i.value)); }
  pct(v: number): number { return Math.max(0, (v / this.max) * 100); }
  colorFor(i: number): string { return this.colors?.[i] ?? 'var(--de-primary)'; }
  fmt(v: number, compact = true): string { return fmtValue(v, this.format, compact); }
  round(v: number): number { return Math.round(v); }
}

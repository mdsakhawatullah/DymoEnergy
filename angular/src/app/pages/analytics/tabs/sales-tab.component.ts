import { Component } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsFilterDto, AnalyticsSalesDto } from '../../../proxy/analytics/models';
import { AnBarListComponent, AnChangeComponent } from '../analytics-ui';
import { AnalyticsTabBase } from './tab-base';

/** Sequential green ramp (one hue, light → dark) for magnitude: heatmap cells and day bars. */
const RAMP = ['#EEF2EC', '#C5E1CF', '#8CC4A2', '#3E9B69', '#0E6B3F', '#0A4D2E'];

@Component({
  selector:    'an-sales-tab',
  templateUrl: './sales-tab.component.html',
  styleUrls:   ['../analytics-shared.css', './sales-tab.component.css'],
  imports:     [SharedModule, AnBarListComponent, AnChangeComponent],
})
export class SalesTabComponent extends AnalyticsTabBase<AnalyticsSalesDto> {
  readonly ramp = RAMP.slice(1, 5);

  protected fetch(f: AnalyticsFilterDto) { return this.api.getSales(f); }

  get maxDay(): number { return Math.max(1, ...(this.data?.daily ?? []).map(d => d.sales)); }

  /** Darker = better day, lighter = quiet (relative to the best day in range). */
  dayColor(sales: number): string {
    const r = sales / this.maxDay;
    return r >= .9 ? RAMP[5] : r >= .45 ? RAMP[4] : r > 0 ? RAMP[2] : RAMP[0];
  }

  dayHeight(sales: number): number { return sales > 0 ? Math.max(4, (sales / this.maxDay) * 100) : 2; }

  /** Show a date label under every Nth bar so they never collide. */
  showDayLabel(i: number): boolean {
    const n = this.data?.daily.length ?? 0;
    const step = n <= 10 ? 1 : n <= 31 ? 5 : n <= 92 ? 14 : 30;
    return i % step === 0;
  }

  get maxCell(): number { return Math.max(1, ...(this.data?.heatmap.cells ?? []).flat()); }

  cellColor(v: number): string {
    if (v <= 0) return RAMP[0];
    const r = v / this.maxCell;
    return r > .8 ? RAMP[5] : r > .55 ? RAMP[4] : r > .3 ? RAMP[3] : r > .1 ? RAMP[2] : RAMP[1];
  }

  hourLabel(h: number): string { return String(h % 12 === 0 ? 12 : h % 12); }

  funnelWidth(count: number): number {
    const first = this.data?.funnel[0]?.count ?? 0;
    return first > 0 ? Math.max(2, (count / first) * 100) : 0;
  }
}

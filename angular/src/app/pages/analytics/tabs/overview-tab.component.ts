import { Component, Input } from '@angular/core';
import { tap } from 'rxjs/operators';
import { NzMessageService } from 'ng-zorro-antd/message';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsDailyDto, AnalyticsFilterDto, AnalyticsOverviewDto, AnalyticsWeekDto } from '../../../proxy/analytics/models';
import { formatTaka } from '../../sales-invoices/invoice-format';
import { AnBarListComponent, AnChangeComponent, CATEGORICAL } from '../analytics-ui';
import { AnalyticsTabBase } from './tab-base';

@Component({
  selector:    'an-overview-tab',
  templateUrl: './overview-tab.component.html',
  styleUrls:   ['../analytics-shared.css', './overview-tab.component.css'],
  imports:     [SharedModule, NzModalModule, AnBarListComponent, AnChangeComponent],
})
export class OverviewTabComponent extends AnalyticsTabBase<AnalyticsOverviewDto> {
  /** "August" — what the headline change is measured against. */
  @Input() previousLabel = 'the period before';

  readonly colors = CATEGORICAL;

  targetOpen   = false;
  targetDraft  = 0;
  savingTarget = false;

  /** Week whose days are drawn below the week cards. */
  selectedWeek: AnalyticsWeekDto | null = null;

  constructor(private message: NzMessageService) { super(); }

  protected fetch(f: AnalyticsFilterDto) {
    return this.api.getOverview(f).pipe(tap(d => {
      // Keep the same week open across reloads; otherwise open the best week (or the latest)
      const keep = this.selectedWeek && d.weeks.find(w => w.from === this.selectedWeek!.from);
      this.selectedWeek = keep ?? d.weeks.find(w => w.isBest) ?? d.weeks[d.weeks.length - 1] ?? null;
    }));
  }

  selectWeek(w: AnalyticsWeekDto): void { this.selectedWeek = w; }

  // ── Day drill-down ────────────────────────────────────────────────────────

  get maxDaySales(): number { return Math.max(1, ...(this.selectedWeek?.days ?? []).map(d => d.sales)); }

  /** Only the best day is dark; the rest stay light so it stands out. */
  isBestDay(d: AnalyticsDailyDto): boolean { return d.sales > 0 && d.sales === this.maxDaySales; }

  dayHeight(d: AnalyticsDailyDto): number { return d.sales > 0 ? Math.max(6, (d.sales / this.maxDaySales) * 100) : 0; }

  /** Bar label: "2.8L" for lakhs, full taka below that. */
  dayValue(v: number): string {
    if (v <= 0) return '';
    return v >= 1e5 ? `${(v / 1e5).toFixed(1)}L` : formatTaka(v);
  }

  /** Amount still needed to reach the target ("৳7.6 lakh to go"). */
  get targetGap(): number { return Math.max(0, this.target - (this.data?.sales ?? 0)); }

  seeWeekInvoices(): void {
    const w = this.selectedWeek;
    if (!w) return;
    this.router.navigate(['/sales-invoices'], { queryParams: { from: w.from.slice(0, 10), to: w.to.slice(0, 10) } });
  }

  /** Target applies to a month; for other periods it is scaled by the number of days. */
  get target(): number {
    const d = this.data;
    if (!d?.monthlyTarget || !this.filter.dateFrom || !this.filter.dateTo) return 0;
    const days = (new Date(this.filter.dateTo).getTime() - new Date(this.filter.dateFrom).getTime()) / 86_400_000;
    return days > 25 && days < 32 ? d.monthlyTarget : Math.round(d.monthlyTarget * days / 30.4);
  }

  get targetPct(): number { return this.target > 0 ? Math.round((this.data!.sales / this.target) * 100) : 0; }

  get periodOver(): boolean { return !!this.filter.dateTo && new Date(this.filter.dateTo) < new Date(); }

  weekBar(sales: number): number {
    const max = Math.max(1, ...(this.data?.weeks ?? []).map(w => w.sales));
    return (sales / max) * 100;
  }

  channelBadge(channel: string): 'online' | 'pos' | 'other' {
    const c = channel.toLowerCase();
    if (c.includes('online')) return 'online';
    if (c.includes('pos') || c.includes('showroom')) return 'pos';
    return 'other';
  }

  channelShort(channel: string): string {
    const kind = this.channelBadge(channel);
    return kind === 'online' ? 'Online' : kind === 'pos' ? 'POS' : channel.split(' ')[0];
  }

  openTarget(): void {
    this.targetDraft = this.data?.monthlyTarget ?? 0;
    this.targetOpen  = true;
  }

  saveTarget(): void {
    this.savingTarget = true;
    this.api.setSalesTarget(this.targetDraft || 0).subscribe({
      next: () => {
        this.savingTarget = false;
        this.targetOpen   = false;
        this.message.success('Monthly target saved.');
        if (this.data) this.data = { ...this.data, monthlyTarget: this.targetDraft || 0 };
      },
      error: () => { this.savingTarget = false; },
    });
  }
}

import { Directive, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs';
import { AnalyticsService } from '../../../proxy/analytics/analytics.service';
import { AnalyticsAlertDto, AnalyticsFilterDto } from '../../../proxy/analytics/models';
import { ReportRunnerService } from '../report-runner.service';
import { fmtValue } from '../analytics-ui';

/** Common plumbing for every Analytics tab: reload on filter change, formatting, actions. */
@Directive()
export abstract class AnalyticsTabBase<T> implements OnChanges {
  @Input({ required: true }) filter!: AnalyticsFilterDto;
  @Input() periodLabel = '';

  protected readonly api    = inject(AnalyticsService);
  protected readonly runner = inject(ReportRunnerService);
  protected readonly router = inject(Router);

  data: T | null = null;
  loading = false;
  loadedAt: Date | null = null;
  private sub?: Subscription;

  protected abstract fetch(filter: AnalyticsFilterDto): Observable<T>;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['filter']) this.reload();
  }

  reload(): void {
    this.sub?.unsubscribe();
    this.loading = true;
    this.sub = this.fetch(this.filter).subscribe({
      next:  d  => { this.data = d; this.loading = false; this.loadedAt = new Date(); },
      error: () => { this.loading = false; },
    });
  }

  money(v: number | null | undefined, compact = true): string { return fmtValue(v, 'money', compact); }
  num(v: number | null | undefined): string { return fmtValue(v, 'number'); }
  pct(v: number | null | undefined): string { return fmtValue(v, 'percent'); }

  /** Alert / follow-up buttons open a report, or navigate within the admin. */
  act(a: AnalyticsAlertDto): void {
    if (a.reportKey) { this.runner.view(a.reportKey, this.filter); return; }
    if (a.link) this.router.navigateByUrl(a.link);
  }

  openReport(key: string): void { this.runner.view(key, this.filter); }
  go(url: string): void { this.router.navigateByUrl(url); }
}

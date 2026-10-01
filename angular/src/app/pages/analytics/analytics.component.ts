import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { SharedModule } from '../../shared/shared.module';
import { AnalyticsService } from '../../proxy/analytics/analytics.service';
import { AnalyticsFilterDto } from '../../proxy/analytics/models';
import { ReportRunnerService } from './report-runner.service';
import { OverviewTabComponent } from './tabs/overview-tab.component';
import { SalesTabComponent } from './tabs/sales-tab.component';
import { ProductsTabComponent } from './tabs/products-tab.component';
import { MoneyTabComponent } from './tabs/money-tab.component';
import { CustomersTabComponent } from './tabs/customers-tab.component';
import { ReportsTabComponent } from './tabs/reports-tab.component';

export type AnalyticsTab = 'overview' | 'sales' | 'products' | 'money' | 'customers' | 'reports';
type PeriodKey = 'this-month' | 'last-month' | 'last-7' | 'last-90' | 'this-year';

/** Report the header "Export" button downloads for each tab. */
const EXPORT_REPORT: Record<AnalyticsTab, string> = {
  overview: 'sales-by-day', sales: 'sales-by-day', products: 'stock-value',
  money: 'collections', customers: 'top-customers', reports: 'sales-by-day',
};

@Component({
  selector:    'app-analytics',
  templateUrl: './analytics.component.html',
  styleUrl:    './analytics.component.css',
  providers:   [ReportRunnerService],
  imports: [
    SharedModule, NzModalModule,
    OverviewTabComponent, SalesTabComponent, ProductsTabComponent,
    MoneyTabComponent, CustomersTabComponent, ReportsTabComponent,
  ],
})
export class AnalyticsComponent implements OnInit {
  private route     = inject(ActivatedRoute);
  private router    = inject(Router);
  private analytics = inject(AnalyticsService);
  readonly runner   = inject(ReportRunnerService);

  readonly tabs: { key: AnalyticsTab; label: string; icon: string }[] = [
    { key: 'overview',  label: 'Overview',         icon: 'dashboard' },
    { key: 'sales',     label: 'Sales',            icon: 'bar-chart' },
    { key: 'products',  label: 'Products & stock', icon: 'appstore' },
    { key: 'money',     label: 'Money',            icon: 'wallet' },
    { key: 'customers', label: 'Customers',        icon: 'team' },
    { key: 'reports',   label: 'Reports',          icon: 'file-text' },
  ];

  readonly periods: { key: PeriodKey; label: string }[] = [
    { key: 'this-month', label: 'This month' },
    { key: 'last-month', label: 'Last month' },
    { key: 'last-7',     label: 'Last 7 days' },
    { key: 'last-90',    label: 'Last 90 days' },
    { key: 'this-year',  label: 'This year' },
  ];

  tab      = signal<AnalyticsTab>('overview');
  period   = signal<PeriodKey>('this-month');
  channel  = signal<string | null>(null);
  channels = signal<string[]>([]);

  /** A new object whenever period/channel change, so tabs reload via ngOnChanges. */
  readonly filter = computed<AnalyticsFilterDto>(() => ({
    ...this.range(this.period()),
    channel: this.channel() ?? undefined,
  }));

  /** "September", "the last 7 days"… used in tab intros. */
  readonly periodLabel = computed(() => {
    const now = new Date();
    switch (this.period()) {
      case 'this-month': return now.toLocaleDateString('en-GB', { month: 'long' });
      case 'last-month': return new Date(now.getFullYear(), now.getMonth() - 1, 1).toLocaleDateString('en-GB', { month: 'long' });
      case 'last-7':     return 'the last 7 days';
      case 'last-90':    return 'the last 90 days';
      default:           return String(now.getFullYear());
    }
  });

  /** What the period is compared with: "August", or "the period before". */
  readonly previousLabel = computed(() => {
    const now = new Date();
    const month = (offset: number) => new Date(now.getFullYear(), now.getMonth() + offset, 1).toLocaleDateString('en-GB', { month: 'long' });
    switch (this.period()) {
      case 'this-month': return month(-1);
      case 'last-month': return month(-2);
      case 'this-year':  return String(now.getFullYear() - 1);
      default:           return 'the period before';
    }
  });


  ngOnInit(): void {
    this.route.queryParamMap.subscribe(q => {
      const t = q.get('tab') as AnalyticsTab | null;
      this.tab.set(t && this.tabs.some(x => x.key === t) ? t : 'overview');
    });
    this.analytics.getChannels().subscribe(c => this.channels.set(c));
  }

  selectTab(t: AnalyticsTab): void {
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab: t === 'overview' ? null : t }, queryParamsHandling: 'merge' });
  }

  exportCurrent(): void {
    this.runner.csv(EXPORT_REPORT[this.tab()], this.filter());
  }

  private range(p: PeriodKey): { dateFrom: string; dateTo: string } {
    const now = new Date();
    const y = now.getFullYear(), m = now.getMonth();
    const iso = (d: Date) => d.toISOString();
    switch (p) {
      case 'last-month': return { dateFrom: iso(new Date(y, m - 1, 1)), dateTo: iso(new Date(y, m, 1, 0, 0, 0, -1)) };
      case 'last-7': {
        const from = new Date(y, m, now.getDate() - 6);
        return { dateFrom: iso(from), dateTo: iso(new Date(y, m, now.getDate() + 1, 0, 0, 0, -1)) };
      }
      case 'last-90': {
        const from = new Date(y, m, now.getDate() - 89);
        return { dateFrom: iso(from), dateTo: iso(new Date(y, m, now.getDate() + 1, 0, 0, 0, -1)) };
      }
      case 'this-year':  return { dateFrom: iso(new Date(y, 0, 1)), dateTo: iso(new Date(y + 1, 0, 1, 0, 0, 0, -1)) };
      default:           return { dateFrom: iso(new Date(y, m, 1)), dateTo: iso(new Date(y, m + 1, 1, 0, 0, 0, -1)) };
    }
  }
}

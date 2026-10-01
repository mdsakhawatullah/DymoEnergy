import { Component } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsFilterDto, AnalyticsProductRowDto, AnalyticsProductsDto } from '../../../proxy/analytics/models';
import { AnBarListComponent } from '../analytics-ui';
import { AnalyticsTabBase } from './tab-base';

@Component({
  selector:    'an-products-tab',
  templateUrl: './products-tab.component.html',
  styleUrls:   ['../analytics-shared.css', './products-tab.component.css'],
  imports:     [SharedModule, AnBarListComponent],
})
export class ProductsTabComponent extends AnalyticsTabBase<AnalyticsProductsDto> {
  protected fetch(f: AnalyticsFilterDto) { return this.api.getProducts(f); }

  runsOut(r: AnalyticsProductRowDto): string {
    if (r.stockState === 'none') return '—';
    if (r.stockState === 'out')  return 'Out now';
    if (r.daysLeft == null)      return 'no recent sales';
    return r.daysLeft > 45 ? '> 45 days' : `about ${Math.ceil(r.daysLeft)} days`;
  }

  daysText(d?: number | null): string {
    if (d == null) return 'no recent sales';
    return d < 1 ? 'less than a day' : `about ${Math.ceil(d)} day${Math.ceil(d) === 1 ? '' : 's'}`;
  }

  /** Product pages are edited from the Products list, filtered by name. */
  openProduct(name: string): void {
    this.router.navigate(['/products'], { queryParams: { search: name } });
  }
}

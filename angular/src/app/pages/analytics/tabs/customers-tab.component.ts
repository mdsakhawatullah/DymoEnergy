import { Component } from '@angular/core';
import { SharedModule } from '../../../shared/shared.module';
import { AnalyticsCustomersDto, AnalyticsFilterDto } from '../../../proxy/analytics/models';
import { AnBarListComponent, AnChangeComponent } from '../analytics-ui';
import { AnalyticsTabBase } from './tab-base';

@Component({
  selector:    'an-customers-tab',
  templateUrl: './customers-tab.component.html',
  styleUrls:   ['../analytics-shared.css'],
  imports:     [SharedModule, AnBarListComponent, AnChangeComponent],
})
export class CustomersTabComponent extends AnalyticsTabBase<AnalyticsCustomersDto> {
  protected fetch(f: AnalyticsFilterDto) { return this.api.getCustomers(f); }
}

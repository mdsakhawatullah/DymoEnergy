import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-stock-entries',
  templateUrl: './stock-entries.component.html',
  imports: [LocalizationPipe],
})
export class StockEntriesComponent {}

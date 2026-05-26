import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-stock-ledger',
  templateUrl: './stock-ledger.component.html',
  imports: [LocalizationPipe],
})
export class StockLedgerComponent {}

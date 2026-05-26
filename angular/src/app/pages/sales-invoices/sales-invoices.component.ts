import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-sales-invoices',
  templateUrl: './sales-invoices.component.html',
  imports: [LocalizationPipe],
})
export class SalesInvoicesComponent {}

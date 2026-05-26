import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-orders',
  templateUrl: './orders.component.html',
  imports: [LocalizationPipe],
})
export class OrdersComponent {}

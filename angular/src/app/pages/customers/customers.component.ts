import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-customers',
  templateUrl: './customers.component.html',
  imports: [LocalizationPipe],
})
export class CustomersComponent {}

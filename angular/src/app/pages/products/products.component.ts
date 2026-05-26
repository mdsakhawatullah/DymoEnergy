import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-products',
  templateUrl: './products.component.html',
  imports: [LocalizationPipe],
})
export class ProductsComponent {}

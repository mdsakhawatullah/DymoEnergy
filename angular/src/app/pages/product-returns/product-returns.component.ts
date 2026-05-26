import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-product-returns',
  templateUrl: './product-returns.component.html',
  imports: [LocalizationPipe],
})
export class ProductReturnsComponent {}

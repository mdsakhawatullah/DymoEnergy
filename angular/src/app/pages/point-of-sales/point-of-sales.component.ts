import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-point-of-sales',
  templateUrl: './point-of-sales.component.html',
  imports: [LocalizationPipe],
})
export class PointOfSalesComponent {}

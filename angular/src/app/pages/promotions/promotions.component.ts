import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-promotions',
  templateUrl: './promotions.component.html',
  imports: [LocalizationPipe],
})
export class PromotionsComponent {}

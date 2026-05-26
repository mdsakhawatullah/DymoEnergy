import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-catalogues',
  templateUrl: './catalogues.component.html',
  imports: [LocalizationPipe],
})
export class CataloguesComponent {}

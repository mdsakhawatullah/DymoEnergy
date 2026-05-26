import { Component } from '@angular/core';
import { LocalizationPipe } from '@abp/ng.core';

@Component({
  selector: 'app-pos-counters',
  templateUrl: './pos-counters.component.html',
  imports: [LocalizationPipe],
})
export class PosCountersComponent {}

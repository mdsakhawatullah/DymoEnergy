import { NgModule } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzPaginationModule } from 'ng-zorro-antd/pagination';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzIconModule } from 'ng-zorro-antd/icon';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzDrawerModule } from 'ng-zorro-antd/drawer';
import { NzFormModule } from 'ng-zorro-antd/form';
import { NzSelectModule } from 'ng-zorro-antd/select';
import { NzSwitchModule } from 'ng-zorro-antd/switch';
import { NzInputNumberModule } from 'ng-zorro-antd/input-number';
import { NzDividerModule } from 'ng-zorro-antd/divider';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzStepsModule } from 'ng-zorro-antd/steps';
import { NzAlertModule } from 'ng-zorro-antd/alert';
import { NzTabsModule } from 'ng-zorro-antd/tabs';
import { NzPopconfirmDirective } from 'ng-zorro-antd/popconfirm';
import { NzTooltipDirective } from 'ng-zorro-antd/tooltip';

const MODULES = [
  CommonModule,
  FormsModule,
  ReactiveFormsModule,
  RouterModule,
  NzTableModule,
  NzPaginationModule,
  NzTagModule,
  NzInputModule,
  NzButtonModule,
  NzIconModule,
  NzSpinModule,
  NzDrawerModule,
  NzFormModule,
  NzSelectModule,
  NzSwitchModule,
  NzInputNumberModule,
  NzDividerModule,
  NzDatePickerModule,
  NzStepsModule,
  NzAlertModule,
  NzTabsModule,
];

// ng-zorro v17+ standalone directives (no module wrappers)
const NZ_STANDALONE = [NzPopconfirmDirective, NzTooltipDirective];

// Standalone pipes/directives that must appear in both imports and exports
const STANDALONE = [DatePipe, LocalizationPipe];

@NgModule({
  imports:  [...MODULES, ...NZ_STANDALONE, ...STANDALONE],
  exports:  [...MODULES, ...NZ_STANDALONE, ...STANDALONE],
})
export class SharedModule {}

import { Component, Input, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { StockLedgerService } from '../../../proxy/stock/stock-ledger.service';
import { LedgerAccessDto, LedgerPersonDto, LedgerSettingDto } from '../../../proxy/stock/ledger.models';
import { money } from '../ledger.utils';

@Component({
  selector: 'app-ledger-access',
  templateUrl: './ledger-access.component.html',
  styleUrls: ['../ledger-shared.css', './ledger-access.component.css'],
  imports: [SharedModule],
})
export class LedgerAccessComponent implements OnInit {
  @Input() canEdit = false;

  money = money;
  data: LedgerAccessDto | null = null;
  setting: LedgerSettingDto | null = null;
  saving = false;

  constructor(private api: StockLedgerService, private message: NzMessageService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getAccess().subscribe(d => {
      this.data = d;
      this.setting = { ...d.setting };
    });
  }

  initials(p: LedgerPersonDto): string {
    return (p.name || '?').split(' ').filter(Boolean).slice(0, 2).map(w => w[0]).join('').toUpperCase();
  }

  get withoutTwoStep(): number {
    return (this.data?.people ?? []).filter(p => !p.twoStep).length;
  }

  save(): void {
    if (!this.setting) return;
    this.saving = true;
    this.api.updateSetting(this.setting).subscribe({
      next: s => {
        this.saving = false;
        this.setting = { ...s };
        this.message.success('Saved.');
      },
      error: () => (this.saving = false),
    });
  }
}

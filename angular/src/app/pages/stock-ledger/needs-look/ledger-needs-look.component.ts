import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { StockLedgerService } from '../../../proxy/stock/stock-ledger.service';
import { LedgerFlaggedDto, LedgerNeedsLookDto, LedgerSettingDto } from '../../../proxy/stock/ledger.models';
import { flagTone, money, signed } from '../ledger.utils';

@Component({
  selector: 'app-ledger-needs-look',
  templateUrl: './ledger-needs-look.component.html',
  styleUrls: ['../ledger-shared.css', './ledger-needs-look.component.css'],
  imports: [SharedModule],
})
export class LedgerNeedsLookComponent implements OnInit {
  @Input() canEdit = false;
  @Output() openLine = new EventEmitter<number>();
  @Output() changed = new EventEmitter<void>();

  money = money;
  signed = signed;

  data: LedgerNeedsLookDto | null = null;
  setting: LedgerSettingDto | null = null;
  saving = false;
  busy = new Set<number>();

  constructor(private api: StockLedgerService, private message: NzMessageService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getNeedsLook().subscribe(d => {
      this.data = d;
      this.setting = { ...d.setting };
    });
  }

  /** Red for the serious flags, amber for the rest. */
  tone(f: LedgerFlaggedDto): string {
    return flagTone(f.line.flags);
  }

  /** The icon beside each flagged line, by what is wrong with it. */
  icon(f: LedgerFlaggedDto): string {
    return { photo: 'bi-camera', 'both-lines': 'bi-arrow-counterclockwise', investigate: 'bi-upc-scan', reset: 'bi-person-x', open: 'bi-clock' }[f.action] ?? 'bi-exclamation-triangle';
  }

  act(f: LedgerFlaggedDto): void {
    if (f.action === 'both-lines' && f.pairLineId) this.openLine.emit(f.pairLineId);
    else this.openLine.emit(f.line.id);
  }

  markChecked(f: LedgerFlaggedDto): void {
    this.busy.add(f.line.id);
    this.api.reviewLine(f.line.id, { action: 'checked' }).subscribe({
      next: () => {
        this.busy.delete(f.line.id);
        this.message.success(`Line ${f.line.id} marked as checked.`);
        this.load();
        this.changed.emit();
      },
      error: () => this.busy.delete(f.line.id),
    });
  }

  save(): void {
    if (!this.setting) return;
    this.saving = true;
    this.api.updateSetting(this.setting).subscribe({
      next: s => {
        this.saving = false;
        this.setting = { ...s };
        this.message.success('Saved. New lines are checked against these rules.');
        this.load();
      },
      error: () => (this.saving = false),
    });
  }
}

import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { StockLedgerService } from '../../../proxy/stock/stock-ledger.service';
import { LedgerCheckResultDto, LedgerProofDto, LedgerSettingDto } from '../../../proxy/stock/ledger.models';
import { shortHash } from '../ledger.utils';

@Component({
  selector: 'app-ledger-proof',
  templateUrl: './ledger-proof.component.html',
  styleUrls: ['../ledger-shared.css', './ledger-proof.component.css'],
  imports: [SharedModule],
})
export class LedgerProofComponent implements OnInit {
  @Input() canEdit = false;
  @Input() verifying = false;
  /** The result of a check started from the page header, so both places agree. */
  @Input() lastResult: LedgerCheckResultDto | null = null;
  @Output() verify = new EventEmitter<void>();
  @Output() export = new EventEmitter<void>();
  @Output() openLine = new EventEmitter<number>();

  shortHash = shortHash;
  data: LedgerProofDto | null = null;
  setting: LedgerSettingDto | null = null;
  saving = false;

  /** What the server records about every line, written by it and not by the browser. */
  readonly carried: string[] = [
    'Date and time to the second', 'The clock it was written on',
    'Quantity before', 'Quantity after',
    'The person’s account', 'Their role at that moment',
    'IP address', 'Browser and device',
    'Sign-in session', 'Whether 2-step was used',
    'The reason typed in', 'The document behind it',
  ];

  constructor(private api: StockLedgerService, private message: NzMessageService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.api.getProof().subscribe(d => {
      this.data = d;
      this.setting = { ...d.setting };
    });
  }

  get check(): LedgerCheckResultDto | null {
    return this.lastResult ?? this.data?.lastCheck ?? null;
  }

  get checkedAgo(): string {
    const at = this.check?.time;
    if (!at) return '';
    const mins = Math.round((Date.now() - new Date(at).getTime()) / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins} minutes ago`;
    const h = Math.round(mins / 60);
    return h < 24 ? `${h} ${h === 1 ? 'hour' : 'hours'} ago` : `${Math.round(h / 24)} days ago`;
  }

  save(): void {
    if (!this.setting) return;
    this.saving = true;
    this.api.updateSetting(this.setting).subscribe({
      next: s => {
        this.saving = false;
        this.setting = { ...s };
        this.message.success('Saved.');
        this.load();
      },
      error: () => (this.saving = false),
    });
  }

  /** A printable sheet of the proof tab, for handing to an auditor. */
  print(): void {
    window.print();
  }
}

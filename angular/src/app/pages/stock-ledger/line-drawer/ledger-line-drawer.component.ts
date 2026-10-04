import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { Router } from '@angular/router';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { StockLedgerService } from '../../../proxy/stock/stock-ledger.service';
import { LedgerLineDetailDto, LedgerPaperDto } from '../../../proxy/stock/ledger.models';
import { money, movementTone, signed } from '../ledger.utils';

@Component({
  selector: 'app-ledger-line-drawer',
  templateUrl: './ledger-line-drawer.component.html',
  styleUrls: ['../ledger-shared.css', './ledger-line-drawer.component.css'],
  imports: [SharedModule],
})
export class LedgerLineDrawerComponent implements OnChanges {
  @Input({ required: true }) lineId!: number;
  @Output() closed = new EventEmitter<void>();
  @Output() openLine = new EventEmitter<number>();
  @Output() reviewed = new EventEmitter<void>();

  detail: LedgerLineDetailDto | null = null;
  failed = false;
  saving = false;

  noteOpen = false;
  note = '';

  money = money;
  signed = signed;
  movementTone = movementTone;

  constructor(private api: StockLedgerService, private message: NzMessageService, private router: Router) {}

  ngOnChanges(): void {
    this.load();
  }

  load(): void {
    this.detail = null;
    this.failed = false;
    this.noteOpen = false;
    this.note = '';
    this.api.getLine(this.lineId).subscribe({
      next: d => (this.detail = d),
      error: () => (this.failed = true),
    });
  }

  copy(text: string | null | undefined, what: string): void {
    if (!text) return;
    navigator.clipboard.writeText(text).then(() => this.message.success(`${what} copied.`), () => this.message.error('Could not copy.'));
  }

  paperIcon(p: LedgerPaperDto): string {
    return { file: 'bi-file-earmark-text', image: 'bi-card-image', undo: 'bi-arrow-counterclockwise', list: 'bi-list-ul' }[p.icon] ?? 'bi-file-earmark';
  }

  follow(p: LedgerPaperDto): void {
    if (p.link === 'ledger-line' && p.linkId) this.openLine.emit(p.linkId);
    else if (p.link === 'stock-entry' && p.linkId) window.open(`/stock-entries/${p.linkId}`, '_blank', 'noopener');
  }

  saveNote(): void {
    if (!this.note.trim()) return void this.message.warning('Write what you want looked at.');
    this.saving = true;
    this.api.reviewLine(this.lineId, { action: 'asked', note: this.note.trim() }).subscribe({
      next: d => {
        this.saving = false;
        this.detail = d;
        this.noteOpen = false;
        this.note = '';
        this.message.success('Noted on this line. It no longer waits on anyone.');
        this.reviewed.emit();
      },
      error: () => (this.saving = false),
    });
  }

  markChecked(): void {
    this.saving = true;
    this.api.reviewLine(this.lineId, { action: 'checked' }).subscribe({
      next: d => {
        this.saving = false;
        this.detail = d;
        this.message.success('Marked as checked.');
        this.reviewed.emit();
      },
      error: () => (this.saving = false),
    });
  }

  /** A reversing line is posted from the stock entry this line came from. */
  reverse(): void {
    const entry = this.detail?.paper.find(p => p.link === 'stock-entry')?.linkId;
    if (!entry) return void this.message.info('This line has no stock entry behind it, so it cannot be reversed from here.');
    this.router.navigate(['/stock-entries', entry]);
    this.closed.emit();
  }
}

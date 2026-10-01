import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { QuoteRequestService } from '../../../proxy/quote-requests/quote-request.service';
import {
  QuoteRequestDto,
  QuoteRequestStatusLabels,
  QuoteRequestStatusOrder,
  QuoteRequestStatusType,
} from '../../../proxy/quote-requests/models';
import { QuoteStatusTone, avatarTone, initials, intlPhone, receivedStamp } from '../quote-format';

interface DetailDraft {
  interest:      string;
  estimatedSize: string;
  monthlyBill:   string;
  roofSite:      string;
  location:      string;
}

@Component({
  selector:    'quote-request-detail',
  templateUrl: './quote-request-detail.component.html',
  styleUrl:    './quote-request-detail.component.css',
  imports:     [SharedModule],
})
export class QuoteRequestDetailComponent implements OnChanges {

  @Input({ required: true }) request!: QuoteRequestDto;
  /** Existing system types, offered as suggestions while editing. */
  @Input() systemTypes: string[] = [];

  @Output() changed = new EventEmitter<QuoteRequestDto>();
  @Output() deleted = new EventEmitter<number>();
  @Output() closed  = new EventEmitter<void>();

  editing = false;
  saving  = false;
  draft: DetailDraft = { interest: '', estimatedSize: '', monthlyBill: '', roofSite: '', location: '' };

  note        = '';
  savingNote  = false;
  changingStatus = false;
  deleting    = false;
  /** Bound to the "Move to…" select; always reset so it acts like a menu. */
  moveTo: QuoteRequestStatusType | null = null;

  readonly statusLabels = QuoteRequestStatusLabels;
  readonly statusTone   = QuoteStatusTone;
  readonly initials     = initials;
  readonly avatarTone   = avatarTone;
  readonly receivedStamp = receivedStamp;

  constructor(
    private quoteRequestService: QuoteRequestService,
    private message: NzMessageService,
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    const prev = changes['request']?.previousValue as QuoteRequestDto | undefined;
    // Only reset local edits when switching to a different request, not on in-place updates
    if (changes['request'] && prev?.id !== this.request.id) {
      this.editing = false;
      this.note    = this.request.adminNote ?? '';
    }
  }

  get moveOptions(): QuoteRequestStatusType[] {
    return QuoteRequestStatusOrder.filter(s => s !== this.request.status);
  }

  get noteDirty(): boolean { return (this.request.adminNote ?? '') !== this.note.trim(); }

  get phoneDigits(): string { return intlPhone(this.request.phone); }

  get whatsAppHref(): string {
    const first = this.request.name.split(' ')[0];
    const text  = `Hi ${first}, this is DymoEnergy regarding your solar quote request` +
                  (this.request.interest ? ` for a ${this.request.interest.toLowerCase()}` : '') + '.';
    return `https://wa.me/${this.phoneDigits}?text=${encodeURIComponent(text)}`;
  }

  get emailHref(): string {
    const subject = `Your solar quote request #${this.request.id}`;
    return `mailto:${this.request.email}?subject=${encodeURIComponent(subject)}`;
  }

  // ── Status ────────────────────────────────────────────────────────────────

  setStatus(status: QuoteRequestStatusType | null): void {
    if (!status || status === this.request.status) return;
    this.changingStatus = true;
    this.quoteRequestService.updateStatus(this.request.id, { status }).subscribe({
      next: updated => {
        this.changingStatus = false;
        this.moveTo = null;
        this.message.success(`Moved to ${this.statusLabels[status]}.`);
        this.changed.emit(updated);
      },
      error: () => { this.changingStatus = false; this.moveTo = null; },
    });
  }

  /** Reaching out from the panel moves a New lead to Contacted automatically. */
  onContact(): void {
    if (this.request.status === 1) this.setStatus(2);
  }

  // ── Requirement edit ─────────────────────────────────────────────────────

  startEdit(): void {
    const r = this.request;
    this.draft = {
      interest:      r.interest      ?? '',
      estimatedSize: r.estimatedSize ?? '',
      monthlyBill:   r.monthlyBill   ?? '',
      roofSite:      r.roofSite      ?? '',
      location:      r.location      ?? '',
    };
    this.editing = true;
  }

  saveDetails(): void {
    this.saving = true;
    this.quoteRequestService.updateDetails(this.request.id, {
      ...this.draft,
      adminNote: this.request.adminNote,
    }).subscribe({
      next: updated => {
        this.saving  = false;
        this.editing = false;
        this.message.success('Requirement updated.');
        this.changed.emit(updated);
      },
      error: () => { this.saving = false; },
    });
  }

  saveNote(): void {
    const r = this.request;
    this.savingNote = true;
    this.quoteRequestService.updateDetails(r.id, {
      interest: r.interest, estimatedSize: r.estimatedSize, monthlyBill: r.monthlyBill,
      roofSite: r.roofSite, location: r.location, adminNote: this.note,
    }).subscribe({
      next: updated => {
        this.savingNote = false;
        this.note = updated.adminNote ?? '';
        this.message.success('Note saved.');
        this.changed.emit(updated);
      },
      error: () => { this.savingNote = false; },
    });
  }

  remove(): void {
    this.deleting = true;
    this.quoteRequestService.delete(this.request.id).subscribe({
      next: () => {
        this.deleting = false;
        this.message.success('Quote request deleted.');
        this.deleted.emit(this.request.id);
      },
      error: () => { this.deleting = false; },
    });
  }
}

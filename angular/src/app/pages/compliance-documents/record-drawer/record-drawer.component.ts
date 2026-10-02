import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { switchMap } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ComplianceService } from '../../../proxy/compliance/compliance.service';
import {
  ComplianceCertificateDto,
  ComplianceFileDto,
  ComplianceFileOwner,
  ComplianceFilingDto,
  ComplianceFilingStatus,
  ComplianceLicenceDto,
  ComplianceListItemDto,
} from '../../../proxy/compliance/models';
import { parseDateKey, toDateKey } from '../../project-planning/project-planning.utils';

export interface RecordDrawerInput {
  type: 'licence' | 'filing' | 'certificate';
  record: ComplianceLicenceDto | ComplianceFilingDto | ComplianceCertificateDto | null;
}

@Component({
  selector: 'compliance-record-drawer',
  templateUrl: './record-drawer.component.html',
  styleUrl: './record-drawer.component.css',
  imports: [SharedModule],
})
export class RecordDrawerComponent implements OnChanges {
  @Input({ required: true }) input!: RecordDrawerInput;
  @Input() files: ComplianceFileDto[] = [];
  @Input() categories: string[] = [];
  @Input() suppliers: string[] = [];
  @Input() badges: ComplianceListItemDto[] = [];

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  readonly filingStatuses = [
    { value: ComplianceFilingStatus.NotStarted, label: 'Not started' },
    { value: ComplianceFilingStatus.InProgress, label: 'In progress' },
    { value: ComplianceFilingStatus.Filed, label: 'Filed' },
    { value: ComplianceFilingStatus.UpToDate, label: 'Up to date' },
  ];

  saving = false;
  uploading = false;
  localFiles: ComplianceFileDto[] = [];

  // licence
  name = '';
  description = '';
  number = '';
  issuedBy = '';
  owner = '';
  hasExpiry = true;
  issuedOn: Date | null = null;
  validUntil: Date | null = null;

  // filing
  title = '';
  detail = '';
  dueDate: Date | null = new Date();
  status = ComplianceFilingStatus.NotStarted;
  actionLabel = '';

  // certificate
  productName = '';
  category = '';
  supplier = '';
  badgeCodes: string[] = [];
  testReport = '';

  constructor(
    private api: ComplianceService,
    private message: NzMessageService,
  ) {}

  get id(): number | null {
    return (this.input.record as { id: number } | null)?.id ?? null;
  }

  get ownerKind(): ComplianceFileOwner | null {
    return this.input.type === 'licence' ? ComplianceFileOwner.Licence : this.input.type === 'certificate' ? ComplianceFileOwner.Certificate : null;
  }

  ngOnChanges(): void {
    const r: any = this.input.record;
    this.localFiles = this.files.filter(f => f.ownerKind === this.ownerKind && f.ownerId === this.id);
    const d = (v?: string | null) => (v ? parseDateKey(v.slice(0, 10)) : null);

    if (this.input.type === 'licence') {
      this.name = r?.name ?? '';
      this.description = r?.description ?? '';
      this.number = r?.number ?? '';
      this.issuedBy = r?.issuedBy ?? '';
      this.owner = r?.owner ?? '';
      this.hasExpiry = r?.hasExpiry ?? true;
      this.issuedOn = d(r?.issuedOn);
      this.validUntil = d(r?.validUntil);
    } else if (this.input.type === 'filing') {
      this.title = r?.title ?? '';
      this.detail = r?.detail ?? '';
      this.dueDate = d(r?.dueDate) ?? new Date();
      this.status = r?.status ?? ComplianceFilingStatus.NotStarted;
      this.owner = r?.owner ?? '';
      this.actionLabel = r?.actionLabel ?? '';
    } else {
      this.productName = r?.productName ?? '';
      this.category = r?.category ?? '';
      this.supplier = r?.supplier ?? '';
      this.badgeCodes = [...(r?.badges ?? [])];
      this.testReport = r?.testReport ?? '';
      this.validUntil = d(r?.validUntil);
    }
  }

  private key(d: Date | null): string | null {
    return d ? toDateKey(d) : null;
  }

  save(): void {
    const t = this.input.type;
    const id = this.id;
    let req$;

    if (t === 'licence') {
      if (!this.name.trim()) return void this.message.warning('Enter the licence name.');
      const body = {
        name: this.name, description: this.description || null, number: this.number || null, issuedBy: this.issuedBy || null,
        owner: this.owner || null, hasExpiry: this.hasExpiry, issuedOn: this.key(this.issuedOn), validUntil: this.key(this.validUntil), order: 0,
      };
      req$ = id ? this.api.updateLicence(id, body) : this.api.createLicence(body);
    } else if (t === 'filing') {
      if (!this.title.trim() || !this.dueDate) return void this.message.warning('Enter a title and a due date.');
      const body = {
        title: this.title, detail: this.detail || null, dueDate: this.key(this.dueDate)!, status: this.status,
        owner: this.owner || null, actionLabel: this.actionLabel || null, order: (this.input.record as ComplianceFilingDto | null)?.order ?? 0,
      };
      req$ = id ? this.api.updateFiling(id, body) : this.api.createFiling(body);
    } else {
      if (!this.productName.trim()) return void this.message.warning('Enter the product name.');
      const body = {
        productName: this.productName, category: this.category || null, supplier: this.supplier || null,
        badges: this.badgeCodes, testReport: this.testReport || null, validUntil: this.key(this.validUntil),
      };
      req$ = id ? this.api.updateCertificate(id, body) : this.api.createCertificate(body);
    }

    this.saving = true;
    req$.subscribe({
      next: () => {
        this.saving = false;
        this.message.success('Saved.');
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }

  remove(): void {
    const id = this.id;
    if (!id) return;
    const t = this.input.type;
    const req$ = t === 'licence' ? this.api.deleteLicence(id) : t === 'filing' ? this.api.deleteFiling(id) : this.api.deleteCertificate(id);
    this.saving = true;
    req$.subscribe({
      next: () => {
        this.saving = false;
        this.message.success('Deleted.');
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }

  // ── Files ─────────────────────────────────────────────────────────────────
  addFile(ev: Event, replace: boolean): void {
    const el = ev.target as HTMLInputElement;
    const file = el.files?.[0];
    el.value = '';
    const kind = this.ownerKind;
    const id = this.id;
    if (!file || kind == null || !id) return;

    this.uploading = true;
    this.api
      .uploadFile(file)
      .pipe(switchMap(up => this.api.addFile({ ownerKind: kind, ownerId: id, fileName: up.fileName, url: up.url, sizeBytes: up.sizeBytes, replaceExisting: replace })))
      .subscribe({
        next: f => {
          this.uploading = false;
          this.localFiles = replace ? [f] : [...this.localFiles, f];
          this.message.success('File attached.');
        },
        error: err => {
          this.uploading = false;
          this.message.error(err?.error?.message ?? 'Upload failed.');
        },
      });
  }

  removeFile(f: ComplianceFileDto): void {
    this.api.deleteFile(f.id).subscribe(() => (this.localFiles = this.localFiles.filter(x => x.id !== f.id)));
  }

  size(bytes: number): string {
    return bytes >= 1048576 ? (bytes / 1048576).toFixed(1) + ' MB' : Math.max(1, Math.round(bytes / 1024)) + ' KB';
  }
}

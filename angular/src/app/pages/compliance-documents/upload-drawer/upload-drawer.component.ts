import { Component, EventEmitter, Input, Output } from '@angular/core';
import { switchMap } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ComplianceService } from '../../../proxy/compliance/compliance.service';
import { ComplianceCertificateDto, ComplianceFileOwner, ComplianceLicenceDto } from '../../../proxy/compliance/models';

@Component({
  selector: 'compliance-upload-drawer',
  templateUrl: './upload-drawer.component.html',
  imports: [SharedModule],
})
export class UploadDrawerComponent {
  @Input() licences: ComplianceLicenceDto[] = [];
  @Input() certificates: ComplianceCertificateDto[] = [];

  @Output() closed = new EventEmitter<void>();
  @Output() uploaded = new EventEmitter<void>();

  /** "licence:3" / "certificate:7" */
  target: string | null = null;
  file: File | null = null;
  replace = false;
  busy = false;

  constructor(
    private api: ComplianceService,
    private message: NzMessageService,
  ) {}

  pick(ev: Event): void {
    this.file = (ev.target as HTMLInputElement).files?.[0] ?? null;
  }

  upload(): void {
    if (!this.target || !this.file) {
      this.message.warning('Choose what the document belongs to, and a file.');
      return;
    }
    const [kind, idText] = this.target.split(':');
    const ownerKind = kind === 'licence' ? ComplianceFileOwner.Licence : ComplianceFileOwner.Certificate;
    const ownerId = +idText;

    this.busy = true;
    this.api
      .uploadFile(this.file)
      .pipe(switchMap(up => this.api.addFile({ ownerKind, ownerId, fileName: up.fileName, url: up.url, sizeBytes: up.sizeBytes, replaceExisting: this.replace })))
      .subscribe({
        next: () => {
          this.busy = false;
          this.message.success('Document uploaded.');
          this.uploaded.emit();
        },
        error: err => {
          this.busy = false;
          this.message.error(err?.error?.message ?? 'Upload failed.');
        },
      });
  }
}

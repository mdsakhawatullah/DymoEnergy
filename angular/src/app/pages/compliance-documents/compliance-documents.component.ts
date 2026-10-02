import { Component, OnInit } from '@angular/core';
import { switchMap } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { ComplianceService } from '../../proxy/compliance/compliance.service';
import {
  ComplianceCertificateDto,
  ComplianceDocStatus,
  ComplianceFileDto,
  ComplianceFileOwner,
  ComplianceFilingDto,
  ComplianceFilingStatus,
  ComplianceItemKind,
  ComplianceLicenceDto,
  ComplianceListItemDto,
  CompliancePageDto,
  ComplianceProjectPackDto,
  ComplianceSectionSummaryDto,
} from '../../proxy/compliance/models';
import { tint } from '../project-planning/project-planning.utils';
import { RecordDrawerComponent, RecordDrawerInput } from './record-drawer/record-drawer.component';
import { ComplianceCustomizeDrawerComponent, CustomizeTab } from './customize-drawer/customize-drawer.component';
import { UploadDrawerComponent } from './upload-drawer/upload-drawer.component';

type TabKey = 'licences' | 'filings' | 'certificates' | 'packs';

const MONTHS = ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG', 'SEP', 'OCT', 'NOV', 'DEC'];
const CATEGORY_TONES = ['#2563EB', '#D97706', '#0E6B3F', '#7C3AED', '#0F766E', '#DB2777'];

@Component({
  selector: 'app-compliance-documents',
  templateUrl: './compliance-documents.component.html',
  styleUrl: './compliance-documents.component.css',
  imports: [SharedModule, RecordDrawerComponent, ComplianceCustomizeDrawerComponent, UploadDrawerComponent],
})
export class ComplianceDocumentsComponent implements OnInit {
  readonly Kind = ComplianceItemKind;
  readonly DocStatus = ComplianceDocStatus;
  readonly FilingStatus = ComplianceFilingStatus;

  page: CompliancePageDto | null = null;
  loading = false;
  tab: TabKey = 'licences';

  // certificate filters
  search = '';
  categoryFilter: string | null = null;
  supplierFilter: string | null = null;

  // drawers
  recordOpen = false;
  recordInput: RecordDrawerInput | null = null;
  customizeOpen = false;
  customizeTab: CustomizeTab = 'page';
  customizeChanged = false;
  uploadOpen = false;
  downloading = false;
  busyIds = new Set<string>();

  constructor(
    private api: ComplianceService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  // ── Loading & labels ──────────────────────────────────────────────────────
  load(): void {
    this.loading = true;
    this.api.getPage().subscribe({
      next: p => {
        this.page = p;
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
  }

  /** Text for a customisable label key. */
  t(key: string): string {
    return this.page?.setting.labels.find(l => l.key === key)?.value ?? '';
  }

  get accent(): string {
    return this.page?.setting.accentColor ?? '#0E6B3F';
  }

  tint = tint;

  items(kind: ComplianceItemKind): ComplianceListItemDto[] {
    return (this.page?.items ?? []).filter(i => i.kind === kind);
  }

  get tabs(): { key: TabKey; label: string; icon: string; badge: number; tone: string }[] {
    const s = this.page?.summary;
    return [
      { key: 'licences', label: this.t('tab.licences'), icon: 'bi-shield-check', badge: s?.licences.badge ?? 0, tone: 'red' },
      { key: 'filings', label: this.t('tab.filings'), icon: 'bi-journal-text', badge: s?.filings.badge ?? 0, tone: 'amber' },
      { key: 'certificates', label: this.t('tab.certificates'), icon: 'bi-patch-check', badge: s?.certificates.badge ?? 0, tone: 'red' },
      { key: 'packs', label: this.t('tab.packs'), icon: 'bi-folder2-open', badge: s?.packs.badge ?? 0, tone: 'amber' },
    ];
  }

  get cards(): { key: TabKey; label: string; s: ComplianceSectionSummaryDto }[] {
    const s = this.page?.summary;
    if (!s) return [];
    return [
      { key: 'licences', label: this.t('card.licences'), s: s.licences },
      { key: 'filings', label: this.t('card.filings'), s: s.filings },
      { key: 'certificates', label: this.t('card.certificates'), s: s.certificates },
      { key: 'packs', label: this.t('card.packs'), s: s.packs },
    ];
  }

  get statusWord(): string {
    const k = this.page?.summary.statusKey ?? 'good';
    return this.t(k === 'good' ? 'status.good' : k === 'almost' ? 'status.almost' : 'status.bad');
  }

  toneColor(tone: string): string {
    switch (tone) {
      case 'red': case 'expired': case 'missing': case 'overdue': return '#B42318';
      case 'amber': case 'soon': case 'unset': return '#9A5B00';
      case 'green': case 'ok': case 'none': case 'done': return '#0B5A34';
      default: return '#5F6B63';
    }
  }

  dotColor(tone: string): string {
    switch (tone) {
      case 'expired': case 'red': return '#B42318';
      case 'soon': case 'amber': return '#D97706';
      case 'unset': return '#9AA39C';
      default: return '#0E6B3F';
    }
  }

  // ── Files ─────────────────────────────────────────────────────────────────
  filesFor(kind: ComplianceFileOwner, id: number): ComplianceFileDto[] {
    return (this.page?.files ?? []).filter(f => f.ownerKind === kind && f.ownerId === id);
  }

  latestFile(kind: ComplianceFileOwner, id: number): ComplianceFileDto | undefined {
    const files = this.filesFor(kind, id);
    return files[files.length - 1];
  }

  licenceFile(l: ComplianceLicenceDto): ComplianceFileDto | undefined {
    return this.latestFile(ComplianceFileOwner.Licence, l.id);
  }

  /** Upload a replacement document for a licence straight from the table row. */
  replaceLicenceFile(l: ComplianceLicenceDto, ev: Event): void {
    const input = ev.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    const key = 'lic' + l.id;
    this.busyIds.add(key);
    this.api
      .uploadFile(file)
      .pipe(
        switchMap(up =>
          this.api.addFile({
            ownerKind: ComplianceFileOwner.Licence,
            ownerId: l.id,
            fileName: up.fileName,
            url: up.url,
            sizeBytes: up.sizeBytes,
            replaceExisting: true,
          }),
        ),
      )
      .subscribe({
        next: () => {
          this.busyIds.delete(key);
          this.message.success('Document saved.');
          this.load();
        },
        error: err => {
          this.busyIds.delete(key);
          this.message.error(err?.error?.message ?? 'Upload failed.');
        },
      });
  }

  downloadAll(): void {
    this.downloading = true;
    this.api.downloadAll().subscribe({
      next: blob => {
        this.downloading = false;
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `compliance-documents-${new Date().toISOString().slice(0, 10)}.zip`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: err => {
        this.downloading = false;
        this.message.warning(err?.status === 404 ? 'There are no uploaded documents yet.' : 'Could not build the ZIP.');
      },
    });
  }

  // ── Licences ──────────────────────────────────────────────────────────────
  openLicence(l: ComplianceLicenceDto | null): void {
    this.openRecord({ type: 'licence', record: l });
  }

  openAlertLicence(): void {
    const id = this.page?.summary.alert?.licenceId;
    const l = this.page?.licences.find(x => x.id === id);
    if (l) this.openLicence(l);
  }

  renewLabel(l: ComplianceLicenceDto): string {
    return l.tone === 'expired' || l.tone === 'soon' ? 'Renew' : 'Replace';
  }

  toggleReminder(r: ComplianceListItemDto, on: boolean): void {
    this.api
      .updateItem(r.id, { kind: r.kind, title: r.title, description: r.description, extra: r.extra, color: r.color, number: r.number, flag: on, order: r.order })
      .subscribe({ next: () => (r.flag = on), error: () => this.load() });
  }

  sections(extra?: string): string[] {
    return (extra ?? '').split(',').map(s => s.trim()).filter(Boolean);
  }

  // ── Filings ───────────────────────────────────────────────────────────────
  openFiling(f: ComplianceFilingDto | null): void {
    this.openRecord({ type: 'filing', record: f });
  }

  get monthFilings(): ComplianceFilingDto[] {
    const now = new Date();
    return (this.page?.filings ?? []).filter(f => this.sameMonth(f.dueDate, now));
  }

  get otherFilings(): ComplianceFilingDto[] {
    const now = new Date();
    return (this.page?.filings ?? []).filter(f => !this.sameMonth(f.dueDate, now));
  }

  private sameMonth(iso: string, now: Date): boolean {
    return +iso.slice(0, 4) === now.getFullYear() && +iso.slice(5, 7) === now.getMonth() + 1;
  }

  get monthName(): string {
    return new Date().toLocaleString('en-GB', { month: 'long', year: 'numeric' });
  }

  dayOf(iso: string): string {
    return String(+iso.slice(8, 10));
  }

  monOf(iso: string): string {
    return MONTHS[+iso.slice(5, 7) - 1];
  }

  filingChip(f: ComplianceFilingDto): { text: string; cls: string } {
    if (f.status === ComplianceFilingStatus.UpToDate) return { text: 'Up to date', cls: 'is-green' };
    if (f.status === ComplianceFilingStatus.Filed) return { text: 'Filed', cls: 'is-green' };
    if (f.tone === 'overdue') return { text: `Overdue ${-f.dueInDays} ${-f.dueInDays === 1 ? 'day' : 'days'}`, cls: 'is-red' };
    if (f.status === ComplianceFilingStatus.InProgress) return { text: 'In progress', cls: 'is-blue' };
    if (f.tone === 'soon') return { text: `Due in ${f.dueInDays} ${f.dueInDays === 1 ? 'day' : 'days'}`, cls: 'is-amber' };
    return { text: 'Not started', cls: 'is-grey' };
  }

  get history(): ComplianceListItemDto[] {
    return this.items(ComplianceItemKind.FilingRecord).slice(-6);
  }

  historyTone(r: ComplianceListItemDto): { word: string; cls: string } {
    const s = (r.extra ?? '').toLowerCase();
    if (s === 'late') return { word: 'Late', cls: 'is-red' };
    if (s === 'due') return { word: 'Due', cls: 'is-amber' };
    return { word: 'Filed', cls: 'is-green' };
  }

  // ── Certificates ──────────────────────────────────────────────────────────
  openCertificate(c: ComplianceCertificateDto | null): void {
    this.openRecord({ type: 'certificate', record: c });
  }

  get categories(): string[] {
    return [...new Set((this.page?.certificates ?? []).map(c => c.category).filter((x): x is string => !!x))].sort();
  }

  get suppliers(): string[] {
    return [...new Set((this.page?.certificates ?? []).map(c => c.supplier).filter((x): x is string => !!x))].sort();
  }

  get filteredCertificates(): ComplianceCertificateDto[] {
    const q = this.search.trim().toLowerCase();
    return (this.page?.certificates ?? []).filter(c =>
      (!this.categoryFilter || c.category === this.categoryFilter) &&
      (!this.supplierFilter || c.supplier === this.supplierFilter) &&
      (!q || [c.productName, c.supplier, c.category, c.testReport, ...c.badges].some(v => (v ?? '').toLowerCase().includes(q))),
    );
  }

  get certAttentionCount(): number {
    return this.filteredCertificates.filter(c => c.needsAttention).length;
  }

  categoryColor(category?: string): string {
    let h = 0;
    for (const ch of category ?? '') h = (h * 31 + ch.charCodeAt(0)) >>> 0;
    return CATEGORY_TONES[h % CATEGORY_TONES.length];
  }

  badgeColor(code: string): string {
    return this.items(ComplianceItemKind.Badge).find(b => b.title === code)?.color || '#5F6B63';
  }

  // ── Import papers ─────────────────────────────────────────────────────────
  get requiredDocs(): string[] {
    return this.sections(this.page?.setting.importRequiredDocs);
  }

  papers(): { item: ComplianceListItemDto; held: string[]; missing: string[] }[] {
    return this.items(ComplianceItemKind.ImportPaper).map(item => {
      const held = this.sections(item.description);
      return { item, held, missing: this.requiredDocs.filter(d => !held.some(h => h.toLowerCase() === d.toLowerCase())) };
    });
  }

  toggleImportDoc(p: { item: ComplianceListItemDto; held: string[] }, doc: string): void {
    const has = p.held.some(h => h.toLowerCase() === doc.toLowerCase());
    const next = has ? p.held.filter(h => h.toLowerCase() !== doc.toLowerCase()) : [...p.held, doc];
    const i = p.item;
    this.api
      .updateItem(i.id, { kind: i.kind, title: i.title, description: next.join(', '), extra: i.extra, color: i.color, number: i.number, flag: i.flag, order: i.order })
      .subscribe(() => this.load());
  }

  // ── Project packs ─────────────────────────────────────────────────────────
  cellOf(pack: ComplianceProjectPackDto, docTypeId: number): ComplianceDocStatus {
    return pack.cells.find(c => c.docTypeId === docTypeId)?.status ?? ComplianceDocStatus.Pending;
  }

  cellSymbol(s: ComplianceDocStatus): string {
    return s === ComplianceDocStatus.Done ? '✓' : s === ComplianceDocStatus.InProgress ? '…' : s === ComplianceDocStatus.Issue ? '!' : '–';
  }

  cellLabel(s: ComplianceDocStatus): string {
    return ['Missing', 'Done', 'In progress', 'Problem'][s];
  }

  /** Click order: missing → done → in progress → problem → missing. */
  cycleCell(pack: ComplianceProjectPackDto, docTypeId: number): void {
    const order = [ComplianceDocStatus.Pending, ComplianceDocStatus.Done, ComplianceDocStatus.InProgress, ComplianceDocStatus.Issue];
    const next = order[(order.indexOf(this.cellOf(pack, docTypeId)) + 1) % order.length];
    const key = `${pack.projectId}|${docTypeId}`;
    if (this.busyIds.has(key)) return;
    this.busyIds.add(key);
    this.api.setProjectDocument(pack.projectId, docTypeId, next).subscribe({
      next: () => {
        this.busyIds.delete(key);
        this.load();
      },
      error: () => this.busyIds.delete(key),
    });
  }

  percentClass(p: number): string {
    return p >= 100 ? 'is-green' : p >= 50 ? 'is-amber' : 'is-grey';
  }

  get handedOverIncomplete(): number {
    return (this.page?.packs ?? []).filter(p => p.isHandedOver && p.donePercent < 100).length;
  }

  // ── Drawers ───────────────────────────────────────────────────────────────
  openRecord(input: RecordDrawerInput): void {
    this.recordInput = input;
    this.recordOpen = true;
  }

  onRecordSaved(): void {
    this.recordOpen = false;
    this.load();
  }

  openCustomize(tab: CustomizeTab = 'page'): void {
    this.customizeTab = tab;
    this.customizeChanged = false;
    this.customizeOpen = true;
  }

  closeCustomize(): void {
    this.customizeOpen = false;
    if (this.customizeChanged) this.load();
  }

  onUploaded(): void {
    this.uploadOpen = false;
    this.load();
  }
}

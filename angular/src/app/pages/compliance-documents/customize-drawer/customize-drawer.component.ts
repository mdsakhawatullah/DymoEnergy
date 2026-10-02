import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { forkJoin, Observable } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ComplianceService } from '../../../proxy/compliance/compliance.service';
import {
  ComplianceItemKind,
  ComplianceListItemDto,
  ComplianceSettingDto,
  CreateUpdateComplianceListItemDto,
} from '../../../proxy/compliance/models';
import { ColorFieldComponent } from '../../project-planning/customize-drawer/color-field.component';

export type CustomizeTab = 'page' | 'badges' | 'doctypes' | 'templates' | 'retention' | 'access' | 'reminders' | 'history' | 'imports';

interface KindConfig {
  tab: CustomizeTab;
  label: string;
  kind: ComplianceItemKind;
  help: string;
  titleLabel: string;
  descriptionLabel?: string;
  extraLabel?: string;
  extraOptions?: { value: string; label: string }[];
  numberLabel?: string;
  flagLabel?: string;
  color?: boolean;
  newTitle: string;
}

const CONFIGS: KindConfig[] = [
  { tab: 'badges', label: 'Badges', kind: ComplianceItemKind.Badge, newTitle: 'New badge', titleLabel: 'Badge code (as written on certificates)', descriptionLabel: 'What it means', color: true,
    help: 'The glossary shown under Product certificates. A product shows a badge in the badge colour when its code matches.' },
  { tab: 'doctypes', label: 'Pack columns', kind: ComplianceItemKind.DocType, newTitle: 'New document', titleLabel: 'Document name',
    help: 'The columns of the document pack every project should end with. Removing a column also clears its ticks.' },
  { tab: 'templates', label: 'Templates', kind: ComplianceItemKind.Template, newTitle: 'New template', titleLabel: 'Template name', descriptionLabel: 'What it covers', extraLabel: 'Version label (e.g. v4 · Sep 2026)', color: true,
    help: 'The standard documents you send to customers.' },
  { tab: 'retention', label: 'Retention', kind: ComplianceItemKind.Retention, newTitle: 'New rule', titleLabel: 'Kind of record', descriptionLabel: 'Detail', numberLabel: 'Keep for (years)',
    help: 'How long each kind of record is kept. Nothing is deleted automatically.' },
  { tab: 'access', label: 'Access', kind: ComplianceItemKind.AccessRule, newTitle: 'New role', titleLabel: 'Role', extraLabel: 'Sections they can open (comma separated)',
    help: 'A reference table of who should see what. Real permissions are set under Administration → Roles.' },
  { tab: 'reminders', label: 'Reminders', kind: ComplianceItemKind.Reminder, newTitle: 'New reminder', titleLabel: 'Reminder', descriptionLabel: 'Who gets told, and when', flagLabel: 'Switched on',
    help: 'The reminder switches on the Licences tab.' },
  { tab: 'history', label: 'Filing history', kind: ComplianceItemKind.FilingRecord, newTitle: 'New month', titleLabel: 'Period (e.g. Jul)', descriptionLabel: 'Detail (e.g. 18 Jul · 3 days late)', numberLabel: 'Year',
    extraLabel: 'Result', extraOptions: [{ value: 'filed', label: 'Filed on time' }, { value: 'late', label: 'Late' }, { value: 'due', label: 'Due' }],
    help: 'One tile per filing period, oldest first. The page shows the last six and counts the last twelve.' },
  { tab: 'imports', label: 'Import papers', kind: ComplianceItemKind.ImportPaper, newTitle: 'New shipment', titleLabel: 'Shipment (e.g. INV-7781 · panels)', descriptionLabel: 'Documents received (comma separated, from the list on the Page tab)', extraLabel: 'Stock entry reference',
    help: 'Shipments and the import documents received for each. You can also tick documents straight from the page.' },
];

interface Row {
  id: number;
  data: CreateUpdateComplianceListItemDto;
  open: boolean;
  saving: boolean;
}

@Component({
  selector: 'compliance-customize-drawer',
  templateUrl: './customize-drawer.component.html',
  styleUrl: './customize-drawer.component.css',
  imports: [SharedModule, ColorFieldComponent],
})
export class ComplianceCustomizeDrawerComponent implements OnInit {
  @Input({ required: true }) setting!: ComplianceSettingDto;
  @Input() items: ComplianceListItemDto[] = [];
  @Input() initialTab: CustomizeTab = 'page';

  @Output() changed = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  readonly configs = CONFIGS;
  tabIndex = 0;

  // page tab
  accent = '#0E6B3F';
  expiringDays = 30;
  importDocs = '';
  labelValues: Record<string, string> = {};
  groups: { name: string; labels: { key: string; caption: string }[] }[] = [];
  savingSetting = false;

  rows: Record<number, Row[]> = {};

  constructor(
    private api: ComplianceService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.accent = this.setting.accentColor;
    this.expiringDays = this.setting.expiringDays;
    this.importDocs = this.setting.importRequiredDocs ?? '';
    for (const l of this.setting.labels) {
      this.labelValues[l.key] = l.value;
      let g = this.groups.find(x => x.name === l.group);
      if (!g) this.groups.push((g = { name: l.group, labels: [] }));
      g.labels.push({ key: l.key, caption: l.caption });
    }
    for (const c of CONFIGS) {
      this.rows[c.kind] = this.items
        .filter(i => i.kind === c.kind)
        .sort((a, b) => a.order - b.order)
        .map(i => this.toRow(i));
    }
    this.tabIndex = this.initialTab === 'page' ? 0 : 1 + CONFIGS.findIndex(c => c.tab === this.initialTab);
  }

  private toRow(i: ComplianceListItemDto): Row {
    return {
      id: i.id,
      open: false,
      saving: false,
      data: { kind: i.kind, title: i.title, description: i.description ?? '', extra: i.extra ?? '', color: i.color ?? '#0E6B3F', number: i.number ?? null, flag: i.flag, order: i.order },
    };
  }

  // ── Page tab ──────────────────────────────────────────────────────────────
  saveSetting(): void {
    this.savingSetting = true;
    this.api
      .updateSetting({ accentColor: this.accent, expiringDays: this.expiringDays, importRequiredDocs: this.importDocs || null, labels: this.labelValues })
      .subscribe({
        next: () => {
          this.savingSetting = false;
          this.message.success('Page settings saved.');
          this.changed.emit();
        },
        error: () => (this.savingSetting = false),
      });
  }

  // ── List tabs ─────────────────────────────────────────────────────────────
  add(cfg: KindConfig): void {
    const list = this.rows[cfg.kind];
    const next = Math.max(0, ...list.map(r => r.data.order)) + 1;
    list.push({
      id: 0,
      open: true,
      saving: false,
      data: { kind: cfg.kind, title: '', description: '', extra: cfg.extraOptions?.[0]?.value ?? '', color: '#0B5A34', number: cfg.numberLabel ? (cfg.kind === ComplianceItemKind.FilingRecord ? new Date().getFullYear() : 1) : null, flag: false, order: next },
    });
  }

  private body(r: Row): CreateUpdateComplianceListItemDto {
    return { ...r.data, description: r.data.description?.trim() || null, extra: r.data.extra?.toString().trim() || null };
  }

  save(cfg: KindConfig, r: Row): void {
    if (!r.data.title.trim()) {
      this.message.warning('Fill in the first field.');
      return;
    }
    r.saving = true;
    const req$ = r.id ? this.api.updateItem(r.id, this.body(r)) : this.api.createItem(this.body(r));
    req$.subscribe({
      next: saved => {
        r.id = saved.id;
        r.saving = false;
        r.open = false;
        this.message.success('Saved.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  remove(cfg: KindConfig, r: Row): void {
    const list = this.rows[cfg.kind];
    if (!r.id) {
      this.rows[cfg.kind] = list.filter(x => x !== r);
      return;
    }
    r.saving = true;
    this.api.deleteItem(r.id).subscribe({
      next: () => {
        this.rows[cfg.kind] = list.filter(x => x !== r);
        this.message.success('Deleted.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  move(cfg: KindConfig, index: number, dir: -1 | 1): void {
    const list = this.rows[cfg.kind];
    const a = list[index];
    const b = list[index + dir];
    if (!a || !b) return;
    [a.data.order, b.data.order] = [b.data.order, a.data.order];
    if (a.data.order === b.data.order) a.data.order += dir;
    [list[index], list[index + dir]] = [b, a];

    const calls: Observable<unknown>[] = [a, b].filter(r => r.id).map(r => this.api.updateItem(r.id, this.body(r)));
    if (calls.length) forkJoin(calls).subscribe({ next: () => this.changed.emit(), error: () => this.message.error('Could not reorder.') });
  }

  summary(cfg: KindConfig, r: Row): string {
    if (cfg.numberLabel && r.data.number != null && cfg.kind === ComplianceItemKind.Retention) return `${r.data.number} yr`;
    if (cfg.kind === ComplianceItemKind.FilingRecord) return `${r.data.number ?? ''} · ${r.data.extra}`;
    return '';
  }
}

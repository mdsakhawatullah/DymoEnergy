import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ProjectPlanningService } from '../../../proxy/project-planning/project-planning.service';
import {
  CreateUpdateProjectSeasonNoteDto,
  CreateUpdateProjectStageDto,
  CreateUpdateProjectTeamDto,
  ProjectPlanningSettingDto,
} from '../../../proxy/project-planning/models';
import { ColorFieldComponent } from './color-field.component';

type TabKey = 'page' | 'stages' | 'teams' | 'season';

interface Row<T> {
  id: number; // 0 = not saved yet
  data: T;
  open: boolean;
  saving: boolean;
}

type StageRow = Row<CreateUpdateProjectStageDto & { checklistText: string }>;
type TeamRow = Row<CreateUpdateProjectTeamDto>;
type NoteRow = Row<CreateUpdateProjectSeasonNoteDto>;

@Component({
  selector: 'customize-drawer',
  templateUrl: './customize-drawer.component.html',
  styleUrl: './customize-drawer.component.css',
  imports: [SharedModule, ColorFieldComponent],
})
export class CustomizeDrawerComponent implements OnInit {
  @Input() initialTab: TabKey = 'page';
  @Input() initialStageId: number | null = null;

  /** Fires after any successful save so the page can refresh behind the drawer. */
  @Output() changed = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  tabIndex = 0;
  loading = true;

  setting: ProjectPlanningSettingDto | null = null;
  savingSetting = false;

  stages: StageRow[] = [];
  teams: TeamRow[] = [];
  notes: NoteRow[] = [];

  private readonly tabOrder: TabKey[] = ['page', 'stages', 'teams', 'season'];

  constructor(
    private api: ProjectPlanningService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.tabIndex = Math.max(0, this.tabOrder.indexOf(this.initialTab));
    this.reload();
  }

  private reload(): void {
    forkJoin({
      setting: this.api.getSetting(),
      stages: this.api.getStages(),
      teams: this.api.getTeams(),
      notes: this.api.getSeasonNotes(),
    }).subscribe({
      next: r => {
        this.setting = r.setting;
        this.stages = r.stages.map(s => ({
          id: s.id,
          open: s.id === this.initialStageId,
          saving: false,
          data: {
            name: s.name,
            color: s.color,
            responsible: s.responsible ?? '',
            durationText: s.durationText ?? '',
            producesText: s.producesText ?? '',
            targetDays: s.targetDays ?? null,
            requiresScheduling: s.requiresScheduling,
            isFinal: s.isFinal,
            order: s.order,
            isActive: s.isActive,
            checklist: s.checklist,
            checklistText: s.checklist.join('\n'),
          },
        }));
        this.teams = r.teams.map(t => ({
          id: t.id,
          open: false,
          saving: false,
          data: { name: t.name, description: t.description ?? '', color: t.color, order: t.order, isActive: t.isActive },
        }));
        this.notes = r.notes.map(n => ({
          id: n.id,
          open: false,
          saving: false,
          data: { title: n.title, period: n.period ?? '', description: n.description ?? '', color: n.color, order: n.order },
        }));
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
  }

  // ── Page labels / colours ─────────────────────────────────────────────────
  saveSetting(): void {
    if (!this.setting) return;
    if (!this.setting.pageTitle?.trim()) {
      this.message.warning('The page needs a title.');
      return;
    }
    this.savingSetting = true;
    this.api.updateSetting(this.setting).subscribe({
      next: s => {
        this.setting = s;
        this.savingSetting = false;
        this.message.success('Page settings saved.');
        this.changed.emit();
      },
      error: () => (this.savingSetting = false),
    });
  }

  // ── Stages ────────────────────────────────────────────────────────────────
  addStage(): void {
    const next = Math.max(0, ...this.stages.map(s => s.data.order)) + 1;
    this.stages = [
      ...this.stages,
      {
        id: 0,
        open: true,
        saving: false,
        data: {
          name: '', color: '#0E6B3F', responsible: '', durationText: '', producesText: '', targetDays: null,
          requiresScheduling: false, isFinal: false, order: next, isActive: true, checklist: [], checklistText: '',
        },
      },
    ];
  }

  private stageDto(r: StageRow): CreateUpdateProjectStageDto {
    const { checklistText, ...rest } = r.data;
    return {
      ...rest,
      responsible: rest.responsible?.trim() || null,
      durationText: rest.durationText?.trim() || null,
      producesText: rest.producesText?.trim() || null,
      checklist: checklistText.split('\n').map(l => l.trim()).filter(Boolean),
    };
  }

  saveStage(r: StageRow): void {
    if (!r.data.name.trim()) {
      this.message.warning('Give the stage a name.');
      return;
    }
    r.saving = true;
    const req$ = r.id ? this.api.updateStage(r.id, this.stageDto(r)) : this.api.createStage(this.stageDto(r));
    req$.subscribe({
      next: saved => {
        r.id = saved.id;
        r.saving = false;
        r.open = false;
        this.message.success('Stage saved.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  deleteStage(r: StageRow): void {
    if (!r.id) {
      this.stages = this.stages.filter(x => x !== r);
      return;
    }
    r.saving = true;
    this.api.deleteStage(r.id).subscribe({
      next: () => {
        this.stages = this.stages.filter(x => x !== r);
        this.message.success('Stage deleted.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  moveStage(index: number, dir: -1 | 1): void {
    this.swap(this.stages, index, dir, r => r.id, r => this.api.updateStage(r.id, this.stageDto(r)));
  }

  // ── Teams ─────────────────────────────────────────────────────────────────
  addTeam(): void {
    const next = Math.max(0, ...this.teams.map(t => t.data.order)) + 1;
    this.teams = [
      ...this.teams,
      { id: 0, open: true, saving: false, data: { name: '', description: '', color: '#0E6B3F', order: next, isActive: true } },
    ];
  }

  private teamDto(r: TeamRow): CreateUpdateProjectTeamDto {
    return { ...r.data, description: r.data.description?.trim() || null };
  }

  saveTeam(r: TeamRow): void {
    if (!r.data.name.trim()) {
      this.message.warning('Give the team a name.');
      return;
    }
    r.saving = true;
    const req$ = r.id ? this.api.updateTeam(r.id, this.teamDto(r)) : this.api.createTeam(this.teamDto(r));
    req$.subscribe({
      next: saved => {
        r.id = saved.id;
        r.saving = false;
        r.open = false;
        this.message.success('Team saved.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  deleteTeam(r: TeamRow): void {
    if (!r.id) {
      this.teams = this.teams.filter(x => x !== r);
      return;
    }
    r.saving = true;
    this.api.deleteTeam(r.id).subscribe({
      next: () => {
        this.teams = this.teams.filter(x => x !== r);
        this.message.success('Team deleted.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  moveTeam(index: number, dir: -1 | 1): void {
    this.swap(this.teams, index, dir, r => r.id, r => this.api.updateTeam(r.id, this.teamDto(r)));
  }

  // ── Season notes ──────────────────────────────────────────────────────────
  addNote(): void {
    const next = Math.max(0, ...this.notes.map(n => n.data.order)) + 1;
    this.notes = [
      ...this.notes,
      { id: 0, open: true, saving: false, data: { title: '', period: '', description: '', color: '#F29D12', order: next } },
    ];
  }

  private noteDto(r: NoteRow): CreateUpdateProjectSeasonNoteDto {
    return { ...r.data, period: r.data.period?.trim() || null, description: r.data.description?.trim() || null };
  }

  saveNote(r: NoteRow): void {
    if (!r.data.title.trim()) {
      this.message.warning('Give the note a title.');
      return;
    }
    r.saving = true;
    const req$ = r.id ? this.api.updateSeasonNote(r.id, this.noteDto(r)) : this.api.createSeasonNote(this.noteDto(r));
    req$.subscribe({
      next: saved => {
        r.id = saved.id;
        r.saving = false;
        r.open = false;
        this.message.success('Season note saved.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  deleteNote(r: NoteRow): void {
    if (!r.id) {
      this.notes = this.notes.filter(x => x !== r);
      return;
    }
    r.saving = true;
    this.api.deleteSeasonNote(r.id).subscribe({
      next: () => {
        this.notes = this.notes.filter(x => x !== r);
        this.message.success('Season note deleted.');
        this.changed.emit();
      },
      error: () => (r.saving = false),
    });
  }

  moveNote(index: number, dir: -1 | 1): void {
    this.swap(this.notes, index, dir, r => r.id, r => this.api.updateSeasonNote(r.id, this.noteDto(r)));
  }

  /** Swap two neighbouring rows' `order` values and persist both (only rows that already exist). */
  private swap<T extends { order: number }>(
    rows: Row<T>[],
    index: number,
    dir: -1 | 1,
    idOf: (r: Row<T>) => number,
    save: (r: Row<T>) => Observable<unknown>,
  ): void {
    const a = rows[index];
    const b = rows[index + dir];
    if (!a || !b) return;

    [a.data.order, b.data.order] = [b.data.order, a.data.order];
    // Orders can be equal after manual edits; make sure the swap really changes the sequence.
    if (a.data.order === b.data.order) a.data.order += dir;
    [rows[index], rows[index + dir]] = [b, a];

    const calls = [a, b].filter(r => idOf(r)).map(r => save(r));
    if (!calls.length) return;
    forkJoin(calls).subscribe({
      next: () => this.changed.emit(),
      error: () => this.reload(),
    });
  }
}

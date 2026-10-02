import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { switchMap } from 'rxjs';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ProjectPlanningService } from '../../../proxy/project-planning/project-planning.service';
import {
  CreateUpdateProjectDto,
  ProjectDto,
  ProjectMaterialStatus,
  ProjectStageDto,
  ProjectTeamDto,
} from '../../../proxy/project-planning/models';
import { dateKey, parseDateKey, toDateKey } from '../project-planning.utils';

const CHECKLIST_CODE = 'DymoEnergy:ProjectChecklistIncomplete';

@Component({
  selector: 'project-drawer',
  templateUrl: './project-drawer.component.html',
  styleUrl: './project-drawer.component.css',
  imports: [SharedModule],
})
export class ProjectDrawerComponent implements OnChanges {
  @Input() project: ProjectDto | null = null;
  @Input() stages: ProjectStageDto[] = [];
  @Input() teams: ProjectTeamDto[] = [];
  @Input() districts: string[] = [];
  @Input() defaultStageId: number | null = null;
  /** Set when a drag-and-drop move was refused because the checklist is incomplete. */
  @Input() pendingMove: { stageId: number; message: string } | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  readonly materialOptions = [
    { value: ProjectMaterialStatus.None, label: 'Not tracked' },
    { value: ProjectMaterialStatus.Ready, label: 'Ready' },
    { value: ProjectMaterialStatus.Short, label: 'Short' },
    { value: ProjectMaterialStatus.OnSite, label: 'On site' },
  ];

  model: CreateUpdateProjectDto = this.blank();
  stageId: number | null = null;
  tagsText = '';
  dueDate: Date | null = null;
  waitingSince: Date | null = null;

  saving = false;
  moveError: string | null = null;
  moveTarget: number | null = null;

  constructor(
    private api: ProjectPlanningService,
    private message: NzMessageService,
  ) {}

  get isEdit(): boolean {
    return !!this.project;
  }

  get currentStage(): ProjectStageDto | undefined {
    return this.stages.find(s => s.id === (this.project?.stageId ?? this.stageId));
  }

  get checklist(): string[] {
    return this.currentStage?.checklist ?? [];
  }

  get doneCount(): number {
    return this.checklist.filter(c => this.isTicked(c)).length;
  }

  get movableStages(): ProjectStageDto[] {
    return this.stages.filter(s => s.id !== this.project?.stageId);
  }

  ngOnChanges(): void {
    const p = this.project;
    this.moveError = this.pendingMove?.message ?? null;
    this.moveTarget = this.pendingMove?.stageId ?? null;
    this.stageId = p?.stageId ?? this.defaultStageId ?? this.stages[0]?.id ?? null;

    if (!p) {
      this.model = this.blank();
      this.tagsText = '';
      this.dueDate = null;
      this.waitingSince = null;
      return;
    }

    this.model = {
      customerName: p.customerName,
      title: p.title ?? null,
      district: p.district ?? null,
      value: p.value,
      stageId: p.stageId,
      teamId: p.teamId ?? null,
      dueDate: null,
      tags: [...p.tags],
      statusNote: p.statusNote ?? null,
      notes: p.notes ?? null,
      isBlocked: p.isBlocked,
      blockedReason: p.blockedReason ?? null,
      waitingFor: p.waitingFor ?? null,
      waitingSince: null,
      actionLabel: p.actionLabel ?? null,
      materialStatus: p.materialStatus,
      materialNote: p.materialNote ?? null,
      checklistDone: [...p.checklistDone],
    };
    this.tagsText = p.tags.join(', ');
    this.dueDate = p.dueDate ? parseDateKey(dateKey(p.dueDate)) : null;
    this.waitingSince = p.waitingSince ? parseDateKey(dateKey(p.waitingSince)) : null;
  }

  private blank(): CreateUpdateProjectDto {
    return {
      customerName: '',
      title: null,
      district: null,
      value: 0,
      stageId: null,
      teamId: null,
      dueDate: null,
      tags: [],
      statusNote: null,
      notes: null,
      isBlocked: false,
      blockedReason: null,
      waitingFor: null,
      waitingSince: null,
      actionLabel: null,
      materialStatus: ProjectMaterialStatus.None,
      materialNote: null,
      checklistDone: [],
    };
  }

  private payload(): CreateUpdateProjectDto {
    return {
      ...this.model,
      customerName: this.model.customerName.trim(),
      stageId: this.isEdit ? this.project!.stageId : this.stageId,
      dueDate: this.dueDate ? toDateKey(this.dueDate) : null,
      waitingSince: this.model.isBlocked && this.waitingSince ? toDateKey(this.waitingSince) : null,
      tags: this.tagsText.split(',').map(t => t.trim()).filter(Boolean),
    };
  }

  // ── Checklist ─────────────────────────────────────────────────────────────
  private key(item: string): string {
    return `${this.currentStage?.id}|${item}`;
  }

  isTicked(item: string): boolean {
    return this.model.checklistDone.includes(this.key(item));
  }

  toggle(item: string, checked: boolean): void {
    const k = this.key(item);
    const rest = this.model.checklistDone.filter(x => x !== k);
    this.model.checklistDone = checked ? [...rest, k] : rest;
  }

  // ── Save / delete ─────────────────────────────────────────────────────────
  save(): void {
    if (!this.model.customerName.trim()) {
      this.message.warning('Enter the customer name.');
      return;
    }
    this.saving = true;
    const body = this.payload();
    const req$ = this.project ? this.api.updateProject(this.project.id, body) : this.api.createProject(body);
    req$.subscribe({
      next: () => {
        this.saving = false;
        this.message.success(this.project ? 'Project updated.' : 'Project created.');
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }

  remove(): void {
    if (!this.project) return;
    this.saving = true;
    this.api.deleteProject(this.project.id).subscribe({
      next: () => {
        this.saving = false;
        this.message.success('Project deleted.');
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }

  // ── Move between stages ───────────────────────────────────────────────────
  /** Saves the form first (so fresh ticks count), then moves the job. */
  move(stageId: number | null, force = false): void {
    if (!this.project || stageId == null) return;
    this.saving = true;
    this.moveError = null;
    const id = this.project.id;

    this.api
      .updateProject(id, this.payload())
      .pipe(switchMap(() => this.api.moveProject(id, { stageId, force })))
      .subscribe({
        next: () => {
          this.saving = false;
          this.message.success('Project moved.');
          this.saved.emit();
        },
        error: err => {
          this.saving = false;
          const apiErr = err?.error?.error;
          if (apiErr?.code === CHECKLIST_CODE) {
            this.moveError = apiErr.message;
            this.moveTarget = stageId;
          } else {
            this.message.error(apiErr?.message ?? 'Could not move the project.');
          }
        },
      });
  }
}

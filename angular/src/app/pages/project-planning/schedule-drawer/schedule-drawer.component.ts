import { Component, EventEmitter, Input, OnChanges, Output } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../../shared/shared.module';
import { ProjectPlanningService } from '../../../proxy/project-planning/project-planning.service';
import { ProjectDto, ProjectTeamDto } from '../../../proxy/project-planning/models';
import { dateKey, parseDateKey, toDateKey } from '../project-planning.utils';

export interface ScheduleDrawerInput {
  projectId: number | null;
  teamId: number | null;
  /** yyyy-MM-dd */
  date: string;
  entryId: number | null;
  note: string | null;
}

@Component({
  selector: 'schedule-drawer',
  templateUrl: './schedule-drawer.component.html',
  styleUrl: './schedule-drawer.component.css',
  imports: [SharedModule],
})
export class ScheduleDrawerComponent implements OnChanges {
  @Input({ required: true }) input!: ScheduleDrawerInput;
  @Input() teams: ProjectTeamDto[] = [];
  @Input() projects: ProjectDto[] = [];

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  projectId: number | null = null;
  teamId: number | null = null;
  date: Date | null = null;
  note = '';
  saving = false;

  constructor(
    private api: ProjectPlanningService,
    private message: NzMessageService,
  ) {}

  ngOnChanges(): void {
    this.projectId = this.input.projectId;
    this.teamId = this.input.teamId ?? this.teams[0]?.id ?? null;
    this.date = this.input.date ? parseDateKey(dateKey(this.input.date)) : new Date();
    this.note = this.input.note ?? '';
  }

  save(): void {
    if (!this.projectId || !this.teamId || !this.date) {
      this.message.warning('Pick a job, a team and a day.');
      return;
    }
    this.saving = true;
    const body = {
      projectId: this.projectId,
      teamId: this.teamId,
      date: toDateKey(this.date),
      note: this.note.trim() || null,
    };
    const req$ = this.input.entryId
      ? this.api.updateScheduleEntry(this.input.entryId, body)
      : this.api.createScheduleEntry(body);

    req$.subscribe({
      next: () => {
        this.saving = false;
        this.message.success(this.input.entryId ? 'Booking updated.' : 'Job scheduled.');
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }

  remove(): void {
    if (!this.input.entryId) return;
    this.saving = true;
    this.api.deleteScheduleEntry(this.input.entryId).subscribe({
      next: () => {
        this.saving = false;
        this.message.success('Booking removed.');
        this.saved.emit();
      },
      error: () => (this.saving = false),
    });
  }
}

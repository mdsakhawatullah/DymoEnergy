import { Component, OnInit } from '@angular/core';
import { NzMessageService } from 'ng-zorro-antd/message';
import { SharedModule } from '../../shared/shared.module';
import { ProjectPlanningService } from '../../proxy/project-planning/project-planning.service';
import {
  ProjectAttentionDto,
  ProjectBoardDto,
  ProjectDto,
  ProjectMaterialStatus,
  ProjectPlanningSettingDto,
  ProjectStageDto,
  ProjectStageSummaryDto,
  ProjectWeekDto,
  ProjectWeekEntryDto,
} from '../../proxy/project-planning/models';
import { ProjectDrawerComponent } from './project-drawer/project-drawer.component';
import { CustomizeDrawerComponent } from './customize-drawer/customize-drawer.component';
import { ScheduleDrawerComponent, ScheduleDrawerInput } from './schedule-drawer/schedule-drawer.component';
import {
  addDays,
  dateKey,
  formatMoney,
  formatRange,
  shortDate,
  tagStyle,
  tint,
  toDateKey,
  weekdayName,
} from './project-planning.utils';

type TabKey = 'board' | 'week' | 'attention' | 'stages';

const DEFAULT_SETTING: ProjectPlanningSettingDto = {
  accentColor: '#0E6B3F',
  currencySymbol: '৳',
  dueSoonDays: 3,
  pageTitle: 'Project planning',
};

@Component({
  selector: 'app-project-planning',
  templateUrl: './project-planning.component.html',
  styleUrl: './project-planning.component.css',
  imports: [SharedModule, ProjectDrawerComponent, CustomizeDrawerComponent, ScheduleDrawerComponent],
})
export class ProjectPlanningComponent implements OnInit {
  readonly MaterialStatus = ProjectMaterialStatus;

  tab: TabKey = 'board';
  loading = false;

  setting: ProjectPlanningSettingDto = DEFAULT_SETTING;
  board: ProjectBoardDto | null = null;
  attention: ProjectAttentionDto | null = null;
  week: ProjectWeekDto | null = null;

  teamFilter: number | null = null;
  districtFilter: string | null = null;
  weekAnchor: string = toDateKey(new Date());

  // ── Drawers ───────────────────────────────────────────────────────────────
  projectDrawerOpen = false;
  selectedProject: ProjectDto | null = null;
  pendingMove: { stageId: number; message: string } | null = null;

  customizeOpen = false;
  customizeTab: 'page' | 'stages' | 'teams' | 'season' = 'page';
  customizeStageId: number | null = null;

  scheduleOpen = false;
  scheduleInput: ScheduleDrawerInput | null = null;

  // ── Drag & drop state ─────────────────────────────────────────────────────
  draggingProjectId: number | null = null;
  dragOverStageId: number | null = null;
  draggingEntry: ProjectWeekEntryDto | null = null;
  dragOverCell: string | null = null;

  constructor(
    private api: ProjectPlanningService,
    private message: NzMessageService,
  ) {}

  ngOnInit(): void {
    this.loadBoard();
  }

  // ── Derived data ──────────────────────────────────────────────────────────
  get tabs(): { key: TabKey; label: string; icon: string; badge?: number }[] {
    const s = this.setting;
    return [
      { key: 'board', label: s.boardTabLabel || 'Board', icon: 'bi-kanban' },
      { key: 'week', label: s.weekTabLabel || 'This week', icon: 'bi-calendar3' },
      { key: 'attention', label: s.attentionTabLabel || 'Needs attention', icon: 'bi-exclamation-triangle', badge: this.board?.attentionCount },
      { key: 'stages', label: s.stagesTabLabel || 'Stages & checklist', icon: 'bi-check2-square' },
    ];
  }

  get stageSummaries(): ProjectStageSummaryDto[] {
    return this.board?.stages ?? [];
  }

  get activeStages(): ProjectStageDto[] {
    return this.stageSummaries.map(s => s.stage).filter(s => s.isActive);
  }

  get weekDays(): { key: string; weekday: string; date: string; isToday: boolean }[] {
    if (!this.week) return [];
    const today = toDateKey(new Date());
    return Array.from({ length: 6 }, (_, i) => {
      const key = addDays(dateKey(this.week!.weekStart), i);
      return { key, weekday: weekdayName(key), date: shortDate(key), isToday: key === today };
    });
  }

  get weekTitle(): string {
    return this.week ? formatRange(dateKey(this.week.weekStart), dateKey(this.week.weekEnd)) : '';
  }

  projectsIn(stageId: number): ProjectDto[] {
    return (this.board?.projects ?? []).filter(p => p.stageId === stageId);
  }

  entriesAt(rowIndex: number, key: string): ProjectWeekEntryDto[] {
    return (this.week?.rows[rowIndex]?.entries ?? []).filter(e => dateKey(e.date) === key);
  }

  stageOf(p: ProjectDto): ProjectStageDto | undefined {
    return this.stageSummaries.find(s => s.stage.id === p.stageId)?.stage;
  }

  /** Days-in-stage colour: normal inside the target, amber above it, red well above it. */
  daysClass(p: ProjectDto): string {
    const target = this.stageOf(p)?.targetDays;
    if (!target) return '';
    if (p.daysInStage > target * 1.5) return 'is-red';
    if (p.daysInStage > target) return 'is-amber';
    return '';
  }

  teamInitial(p: ProjectDto): string {
    return (p.teamName ?? '').trim().charAt(0).toUpperCase();
  }

  teamBadge(name: string): string {
    const words = name.trim().split(/\s+/);
    return words.length > 1 ? (words[0][0] + words[words.length - 1][0]).toUpperCase() : name.trim().slice(0, 2).toUpperCase();
  }

  money = (v: number) => formatMoney(v, this.setting.currencySymbol);
  tint = tint;
  tagStyle = tagStyle;
  shortDate = shortDate;

  timeBar(t: { averageDays?: number | null; targetDays?: number | null }, max: number): { width: string; color: string } {
    const avg = t.averageDays ?? 0;
    const target = t.targetDays ?? 0;
    let color = '#0E6B3F';
    if (target && avg > target * 1.5) color = '#B42318';
    else if (target && avg > target) color = '#D97706';
    return { width: max ? `${Math.max(3, (avg / max) * 100)}%` : '0%', color };
  }

  maxTime(): number {
    return Math.max(0, ...(this.attention?.timePerStage ?? []).map(t => Math.max(t.averageDays ?? 0, t.targetDays ?? 0)));
  }

  waitingClass(days: number): string {
    return days >= 7 ? 'is-red' : days >= 4 ? 'is-amber' : '';
  }

  // ── Loading ───────────────────────────────────────────────────────────────
  private get filter() {
    return { teamId: this.teamFilter, district: this.districtFilter };
  }

  loadBoard(): void {
    this.loading = true;
    this.api.getBoard(this.filter).subscribe({
      next: board => {
        this.board = board;
        this.setting = board.setting;
        this.loading = false;
        if (this.tab === 'attention') this.loadAttention();
        if (this.tab === 'week') this.loadWeek();
      },
      error: () => (this.loading = false),
    });
  }

  loadAttention(): void {
    this.api.getAttention(this.filter).subscribe(a => (this.attention = a));
  }

  loadWeek(): void {
    this.api.getWeek({ ...this.filter, date: this.weekAnchor }).subscribe(w => (this.week = w));
  }

  /** Reload whatever the current tab shows plus the shared header strip. */
  refresh(): void {
    this.loadBoard();
  }

  setTab(tab: TabKey): void {
    this.tab = tab;
    if (tab === 'attention') this.loadAttention();
    if (tab === 'week') this.loadWeek();
  }

  onFilterChange(): void {
    this.loadBoard();
  }

  shiftWeek(weeks: number): void {
    this.weekAnchor = addDays(this.weekAnchor, weeks * 7);
    this.loadWeek();
  }

  thisWeek(): void {
    this.weekAnchor = toDateKey(new Date());
    this.loadWeek();
  }

  // ── Project drawer ────────────────────────────────────────────────────────
  openNewProject(stageId?: number): void {
    this.selectedProject = null;
    this.pendingMove = null;
    this.newProjectStageId = stageId ?? null;
    this.projectDrawerOpen = true;
  }
  newProjectStageId: number | null = null;

  openProject(p: ProjectDto): void {
    this.selectedProject = p;
    this.pendingMove = null;
    this.projectDrawerOpen = true;
  }

  closeProjectDrawer(): void {
    this.projectDrawerOpen = false;
    this.pendingMove = null;
  }

  onProjectSaved(): void {
    this.projectDrawerOpen = false;
    this.pendingMove = null;
    this.refresh();
  }

  // ── Customise drawer ──────────────────────────────────────────────────────
  customizeChanged = false;

  openCustomize(tab: 'page' | 'stages' | 'teams' | 'season' = 'page', stageId: number | null = null): void {
    this.customizeTab = tab;
    this.customizeStageId = stageId;
    this.customizeChanged = false;
    this.customizeOpen = true;
  }

  closeCustomize(): void {
    this.customizeOpen = false;
    if (this.customizeChanged) this.refresh();
  }

  // ── Schedule drawer ───────────────────────────────────────────────────────
  openSchedule(input: ScheduleDrawerInput): void {
    this.scheduleInput = input;
    this.scheduleOpen = true;
  }

  scheduleProject(p: ProjectDto): void {
    this.openSchedule({ projectId: p.id, teamId: p.teamId ?? null, date: toDateKey(new Date()), entryId: null, note: null });
  }

  scheduleCell(teamId: number, key: string): void {
    this.openSchedule({ projectId: null, teamId, date: key, entryId: null, note: null });
  }

  editEntry(e: ProjectWeekEntryDto): void {
    this.openSchedule({ projectId: e.projectId, teamId: e.teamId, date: dateKey(e.date), entryId: e.id, note: e.note ?? null });
  }

  onScheduleSaved(): void {
    this.scheduleOpen = false;
    this.refresh();
  }

  // ── Board drag & drop ─────────────────────────────────────────────────────
  onCardDragStart(ev: DragEvent, p: ProjectDto): void {
    this.draggingProjectId = p.id;
    ev.dataTransfer?.setData('text/plain', String(p.id));
    if (ev.dataTransfer) ev.dataTransfer.effectAllowed = 'move';
  }

  onCardDragEnd(): void {
    this.draggingProjectId = null;
    this.dragOverStageId = null;
  }

  onColumnDragOver(ev: DragEvent, stageId: number): void {
    if (this.draggingProjectId == null) return;
    ev.preventDefault();
    this.dragOverStageId = stageId;
  }

  onColumnDrop(ev: DragEvent, stageId: number): void {
    ev.preventDefault();
    const id = this.draggingProjectId;
    this.onCardDragEnd();
    if (id == null) return;
    const project = this.board?.projects.find(p => p.id === id);
    if (!project || project.stageId === stageId) return;

    this.api.moveProject(id, { stageId, force: false }).subscribe({
      next: () => {
        this.message.success('Project moved.');
        this.refresh();
      },
      error: err => {
        const apiErr = err?.error?.error;
        if (apiErr?.code === 'DymoEnergy:ProjectChecklistIncomplete') {
          // Hand over to the drawer, which shows the checklist and offers "Move anyway".
          this.selectedProject = project;
          this.pendingMove = { stageId, message: apiErr.message };
          this.projectDrawerOpen = true;
        } else {
          this.message.error(apiErr?.message ?? 'Could not move the project.');
        }
      },
    });
  }

  // ── Week drag & drop ──────────────────────────────────────────────────────
  onEntryDragStart(ev: DragEvent, e: ProjectWeekEntryDto): void {
    this.draggingEntry = e;
    ev.dataTransfer?.setData('text/plain', String(e.id));
    if (ev.dataTransfer) ev.dataTransfer.effectAllowed = 'move';
  }

  onEntryDragEnd(): void {
    this.draggingEntry = null;
    this.dragOverCell = null;
  }

  onCellDragOver(ev: DragEvent, teamId: number, key: string): void {
    if (!this.draggingEntry) return;
    ev.preventDefault();
    this.dragOverCell = `${teamId}|${key}`;
  }

  onCellDrop(ev: DragEvent, teamId: number, key: string): void {
    ev.preventDefault();
    const entry = this.draggingEntry;
    this.onEntryDragEnd();
    if (!entry || (entry.teamId === teamId && dateKey(entry.date) === key)) return;

    this.api
      .updateScheduleEntry(entry.id, { projectId: entry.projectId, teamId, date: key, note: entry.note })
      .subscribe({
        next: () => this.loadWeek(),
        error: () => this.message.error('Could not move the booking.'),
      });
  }

  // ── Materials ─────────────────────────────────────────────────────────────
  markMaterialsReady(p: ProjectDto): void {
    this.api.updateProjectMaterial(p.id, { status: ProjectMaterialStatus.Ready, note: p.materialNote }).subscribe({
      next: () => {
        this.message.success('Marked ready.');
        this.loadWeek();
      },
    });
  }
}

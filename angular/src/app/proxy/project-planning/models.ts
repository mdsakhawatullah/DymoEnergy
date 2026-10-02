export enum ProjectMaterialStatus {
  None = 0,
  Ready = 1,
  Short = 2,
  OnSite = 3,
}

export interface ProjectPlanningSettingDto {
  accentColor: string;
  currencySymbol: string;
  dueSoonDays: number;
  pageTitle: string;
  pageSubtitle?: string;
  newProjectLabel?: string;
  boardTabLabel?: string;
  weekTabLabel?: string;
  attentionTabLabel?: string;
  stagesTabLabel?: string;
  stagesIntro?: string;
  attentionTitle?: string;
  scheduleTitle?: string;
  timeTitle?: string;
  timeFootnote?: string;
  seasonTitle?: string;
  seasonSubtitle?: string;
}

export interface ProjectStageDto {
  id: number;
  name: string;
  color: string;
  responsible?: string;
  durationText?: string;
  producesText?: string;
  targetDays?: number | null;
  requiresScheduling: boolean;
  isFinal: boolean;
  order: number;
  isActive: boolean;
  checklist: string[];
}

export interface CreateUpdateProjectStageDto {
  name: string;
  color: string;
  responsible?: string | null;
  durationText?: string | null;
  producesText?: string | null;
  targetDays?: number | null;
  requiresScheduling: boolean;
  isFinal: boolean;
  order: number;
  isActive: boolean;
  checklist: string[];
}

export interface ProjectTeamDto {
  id: number;
  name: string;
  description?: string;
  color: string;
  order: number;
  isActive: boolean;
}

export interface CreateUpdateProjectTeamDto {
  name: string;
  description?: string | null;
  color: string;
  order: number;
  isActive: boolean;
}

export interface ProjectSeasonNoteDto {
  id: number;
  title: string;
  period?: string;
  description?: string;
  color: string;
  order: number;
}

export interface CreateUpdateProjectSeasonNoteDto {
  title: string;
  period?: string | null;
  description?: string | null;
  color: string;
  order: number;
}

export interface ProjectDto {
  id: number;
  code: string;
  customerName: string;
  title?: string;
  district?: string;
  value: number;
  stageId: number;
  stageName: string;
  stageColor: string;
  daysInStage: number;
  teamId?: number | null;
  teamName?: string;
  teamColor?: string;
  dueDate?: string | null;
  nextScheduleDate?: string | null;
  tags: string[];
  statusNote?: string;
  notes?: string;
  isBlocked: boolean;
  blockedReason?: string;
  waitingFor?: string;
  waitingSince?: string | null;
  actionLabel?: string;
  waitingDays: number;
  isLate: boolean;
  isDueSoon: boolean;
  materialStatus: ProjectMaterialStatus;
  materialNote?: string;
  checklistDone: string[];
}

export interface CreateUpdateProjectDto {
  customerName: string;
  title?: string | null;
  district?: string | null;
  value: number;
  stageId?: number | null;
  teamId?: number | null;
  dueDate?: string | null;
  tags: string[];
  statusNote?: string | null;
  notes?: string | null;
  isBlocked: boolean;
  blockedReason?: string | null;
  waitingFor?: string | null;
  waitingSince?: string | null;
  actionLabel?: string | null;
  materialStatus: ProjectMaterialStatus;
  materialNote?: string | null;
  checklistDone: string[];
}

export interface MoveProjectStageDto {
  stageId: number;
  force: boolean;
}

export interface UpdateProjectMaterialDto {
  status: ProjectMaterialStatus;
  note?: string | null;
}

export interface ProjectPlanningFilterDto {
  teamId?: number | null;
  district?: string | null;
}

export interface ProjectWeekFilterDto extends ProjectPlanningFilterDto {
  date?: string | null;
}

export interface ProjectStageSummaryDto {
  stage: ProjectStageDto;
  count: number;
  totalValue: number;
}

export interface ProjectBoardDto {
  setting: ProjectPlanningSettingDto;
  stages: ProjectStageSummaryDto[];
  projects: ProjectDto[];
  teams: ProjectTeamDto[];
  districts: string[];
  attentionCount: number;
}

export interface ProjectStageTimeDto {
  stageId: number;
  name: string;
  color: string;
  averageDays?: number | null;
  targetDays?: number | null;
}

export interface ProjectAttentionDto {
  running: number;
  runningValue: number;
  blocked: number;
  late: number;
  installsThisWeek: number;
  installTeams: number;
  items: ProjectDto[];
  timePerStage: ProjectStageTimeDto[];
  seasonNotes: ProjectSeasonNoteDto[];
}

export interface ProjectWeekEntryDto {
  id: number;
  projectId: number;
  teamId: number;
  date: string;
  title: string;
  note?: string;
  color: string;
  isBlocked: boolean;
}

export interface ProjectWeekRowDto {
  team: ProjectTeamDto;
  entries: ProjectWeekEntryDto[];
}

export interface ProjectWeekDto {
  weekStart: string;
  weekEnd: string;
  rows: ProjectWeekRowDto[];
  waiting: ProjectDto[];
  materials: ProjectDto[];
}

export interface CreateUpdateProjectScheduleEntryDto {
  projectId: number;
  teamId: number;
  date: string;
  note?: string | null;
}

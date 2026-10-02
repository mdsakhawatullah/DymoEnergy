import { RestService } from '@abp/ng.core';
import { Injectable } from '@angular/core';
import type {
  CreateUpdateProjectDto,
  CreateUpdateProjectScheduleEntryDto,
  CreateUpdateProjectSeasonNoteDto,
  CreateUpdateProjectStageDto,
  CreateUpdateProjectTeamDto,
  MoveProjectStageDto,
  ProjectAttentionDto,
  ProjectBoardDto,
  ProjectDto,
  ProjectPlanningFilterDto,
  ProjectPlanningSettingDto,
  ProjectSeasonNoteDto,
  ProjectStageDto,
  ProjectTeamDto,
  ProjectWeekDto,
  ProjectWeekFilterDto,
  UpdateProjectMaterialDto,
} from './models';

const BASE = '/api/app/project-planning';

@Injectable({ providedIn: 'root' })
export class ProjectPlanningService {
  apiName = 'Default';

  constructor(private restService: RestService) {}

  private req<T>(request: { method: string; url: string; params?: any; body?: any }, skipHandleError = false) {
    return this.restService.request<any, T>(request, { apiName: this.apiName, skipHandleError });
  }

  // ── Page views ──────────────────────────────────────────────────────────
  getBoard = (input: ProjectPlanningFilterDto) =>
    this.req<ProjectBoardDto>({ method: 'GET', url: `${BASE}/board`, params: { teamId: input.teamId, district: input.district } });

  getAttention = (input: ProjectPlanningFilterDto) =>
    this.req<ProjectAttentionDto>({ method: 'GET', url: `${BASE}/attention`, params: { teamId: input.teamId, district: input.district } });

  getWeek = (input: ProjectWeekFilterDto) =>
    this.req<ProjectWeekDto>({ method: 'GET', url: `${BASE}/week`, params: { teamId: input.teamId, district: input.district, date: input.date } });

  // ── Settings ────────────────────────────────────────────────────────────
  getSetting = () => this.req<ProjectPlanningSettingDto>({ method: 'GET', url: `${BASE}/setting` });
  updateSetting = (input: ProjectPlanningSettingDto) =>
    this.req<ProjectPlanningSettingDto>({ method: 'PUT', url: `${BASE}/setting`, body: input });

  // ── Stages ──────────────────────────────────────────────────────────────
  getStages = () => this.req<ProjectStageDto[]>({ method: 'GET', url: `${BASE}/stages` });
  createStage = (input: CreateUpdateProjectStageDto) =>
    this.req<ProjectStageDto>({ method: 'POST', url: `${BASE}/stage`, body: input });
  updateStage = (id: number, input: CreateUpdateProjectStageDto) =>
    this.req<ProjectStageDto>({ method: 'PUT', url: `${BASE}/${id}/stage`, body: input });
  deleteStage = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/stage` });

  // ── Teams ───────────────────────────────────────────────────────────────
  getTeams = () => this.req<ProjectTeamDto[]>({ method: 'GET', url: `${BASE}/teams` });
  createTeam = (input: CreateUpdateProjectTeamDto) =>
    this.req<ProjectTeamDto>({ method: 'POST', url: `${BASE}/team`, body: input });
  updateTeam = (id: number, input: CreateUpdateProjectTeamDto) =>
    this.req<ProjectTeamDto>({ method: 'PUT', url: `${BASE}/${id}/team`, body: input });
  deleteTeam = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/team` });

  // ── Season notes ────────────────────────────────────────────────────────
  getSeasonNotes = () => this.req<ProjectSeasonNoteDto[]>({ method: 'GET', url: `${BASE}/season-notes` });
  createSeasonNote = (input: CreateUpdateProjectSeasonNoteDto) =>
    this.req<ProjectSeasonNoteDto>({ method: 'POST', url: `${BASE}/season-note`, body: input });
  updateSeasonNote = (id: number, input: CreateUpdateProjectSeasonNoteDto) =>
    this.req<ProjectSeasonNoteDto>({ method: 'PUT', url: `${BASE}/${id}/season-note`, body: input });
  deleteSeasonNote = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/season-note` });

  // ── Projects ────────────────────────────────────────────────────────────
  getProject = (id: number) => this.req<ProjectDto>({ method: 'GET', url: `${BASE}/${id}/project` });
  createProject = (input: CreateUpdateProjectDto) =>
    this.req<ProjectDto>({ method: 'POST', url: `${BASE}/project`, body: input });
  updateProject = (id: number, input: CreateUpdateProjectDto) =>
    this.req<ProjectDto>({ method: 'PUT', url: `${BASE}/${id}/project`, body: input });
  deleteProject = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/project` });
  moveProject = (id: number, input: MoveProjectStageDto) =>
    this.req<ProjectDto>({ method: 'PUT', url: `${BASE}/${id}/project-stage`, body: input }, true);
  updateProjectMaterial = (id: number, input: UpdateProjectMaterialDto) =>
    this.req<ProjectDto>({ method: 'PUT', url: `${BASE}/${id}/project-material`, body: input });

  // ── Crew schedule ───────────────────────────────────────────────────────
  createScheduleEntry = (input: CreateUpdateProjectScheduleEntryDto) =>
    this.req<void>({ method: 'POST', url: `${BASE}/schedule-entry`, body: input });
  updateScheduleEntry = (id: number, input: CreateUpdateProjectScheduleEntryDto) =>
    this.req<void>({ method: 'PUT', url: `${BASE}/${id}/schedule-entry`, body: input });
  deleteScheduleEntry = (id: number) => this.req<void>({ method: 'DELETE', url: `${BASE}/${id}/schedule-entry` });
}

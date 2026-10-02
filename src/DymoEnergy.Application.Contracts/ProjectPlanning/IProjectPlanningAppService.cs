using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace DymoEnergy.ProjectPlanning;

public interface IProjectPlanningAppService : IApplicationService
{
    // ── Page views ────────────────────────────────────────────────────────
    Task<ProjectBoardDto>     GetBoardAsync(ProjectPlanningFilterDto input);
    Task<ProjectAttentionDto> GetAttentionAsync(ProjectPlanningFilterDto input);
    Task<ProjectWeekDto>      GetWeekAsync(ProjectWeekFilterDto input);

    // ── Customisation ─────────────────────────────────────────────────────
    Task<ProjectPlanningSettingDto> GetSettingAsync();
    Task<ProjectPlanningSettingDto> UpdateSettingAsync(ProjectPlanningSettingDto input);

    Task<List<ProjectStageDto>> GetStagesAsync();
    Task<ProjectStageDto>       CreateStageAsync(CreateUpdateProjectStageDto input);
    Task<ProjectStageDto>       UpdateStageAsync(int id, CreateUpdateProjectStageDto input);
    Task                        DeleteStageAsync(int id);

    Task<List<ProjectTeamDto>> GetTeamsAsync();
    Task<ProjectTeamDto>       CreateTeamAsync(CreateUpdateProjectTeamDto input);
    Task<ProjectTeamDto>       UpdateTeamAsync(int id, CreateUpdateProjectTeamDto input);
    Task                       DeleteTeamAsync(int id);

    Task<List<ProjectSeasonNoteDto>> GetSeasonNotesAsync();
    Task<ProjectSeasonNoteDto>       CreateSeasonNoteAsync(CreateUpdateProjectSeasonNoteDto input);
    Task<ProjectSeasonNoteDto>       UpdateSeasonNoteAsync(int id, CreateUpdateProjectSeasonNoteDto input);
    Task                             DeleteSeasonNoteAsync(int id);

    // ── Projects ──────────────────────────────────────────────────────────
    Task<ProjectDto> GetProjectAsync(int id);
    Task<ProjectDto> CreateProjectAsync(CreateUpdateProjectDto input);
    Task<ProjectDto> UpdateProjectAsync(int id, CreateUpdateProjectDto input);
    Task             DeleteProjectAsync(int id);
    Task<ProjectDto> UpdateProjectStageAsync(int id, MoveProjectStageDto input);
    Task<ProjectDto> UpdateProjectMaterialAsync(int id, UpdateProjectMaterialDto input);

    // ── Crew schedule ─────────────────────────────────────────────────────
    Task CreateScheduleEntryAsync(CreateUpdateProjectScheduleEntryDto input);
    Task UpdateScheduleEntryAsync(int id, CreateUpdateProjectScheduleEntryDto input);
    Task DeleteScheduleEntryAsync(int id);
}

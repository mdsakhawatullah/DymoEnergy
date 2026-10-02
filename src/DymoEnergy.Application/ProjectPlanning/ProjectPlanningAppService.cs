using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DymoEnergy.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace DymoEnergy.ProjectPlanning;

[Authorize(DymoEnergyPermissions.ProjectPlanning.Default)]
public class ProjectPlanningAppService : ApplicationService, IProjectPlanningAppService
{
    public const string ChecklistIncompleteCode = "DymoEnergy:ProjectChecklistIncomplete";

    private static readonly SemaphoreSlim SeedLock = new(1, 1);

    private readonly IRepository<ProjectStage, int>          _stages;
    private readonly IRepository<ProjectTeam, int>           _teams;
    private readonly IRepository<Project, int>               _projects;
    private readonly IRepository<ProjectScheduleEntry, int>  _entries;
    private readonly IRepository<ProjectStageHistory, int>   _history;
    private readonly IRepository<ProjectSeasonNote, int>     _notes;
    private readonly IRepository<ProjectPlanningSetting, int> _settings;

    public ProjectPlanningAppService(
        IRepository<ProjectStage, int>           stages,
        IRepository<ProjectTeam, int>            teams,
        IRepository<Project, int>                projects,
        IRepository<ProjectScheduleEntry, int>   entries,
        IRepository<ProjectStageHistory, int>    history,
        IRepository<ProjectSeasonNote, int>      notes,
        IRepository<ProjectPlanningSetting, int> settings)
    {
        _stages   = stages;
        _teams    = teams;
        _projects = projects;
        _entries  = entries;
        _history  = history;
        _notes    = notes;
        _settings = settings;
    }

    // ══ PAGE VIEWS ═══════════════════════════════════════════════════════════

    public async Task<ProjectBoardDto> GetBoardAsync(ProjectPlanningFilterDto input)
    {
        await EnsureDefaultsAsync();

        var setting  = await LoadSettingAsync();
        var stages   = await _stages.GetListAsync();
        var teams    = await _teams.GetListAsync();
        var allProjects = await _projects.GetListAsync();

        var filtered = ApplyFilter(allProjects, input);
        var dtos     = await BuildProjectDtosAsync(filtered, stages, teams, setting);

        var visibleStages = stages
            .Where(s => s.IsActive || dtos.Any(p => p.StageId == s.Id))
            .OrderBy(s => s.Order).ThenBy(s => s.Id)
            .ToList();

        return new ProjectBoardDto
        {
            Setting  = MapSetting(setting),
            Teams    = teams.Where(t => t.IsActive).OrderBy(t => t.Order).ThenBy(t => t.Id).Select(MapTeam).ToList(),
            Districts = allProjects.Select(p => p.District).Where(d => !string.IsNullOrWhiteSpace(d))
                                   .Select(d => d!).Distinct().OrderBy(d => d).ToList(),
            Projects = dtos,
            Stages   = visibleStages.Select(s => new ProjectStageSummaryDto
            {
                Stage      = MapStage(s),
                Count      = dtos.Count(p => p.StageId == s.Id),
                TotalValue = dtos.Where(p => p.StageId == s.Id).Sum(p => p.Value),
            }).ToList(),
            AttentionCount = dtos.Count(p => NeedsAttention(p, stages)),
        };
    }

    public async Task<ProjectAttentionDto> GetAttentionAsync(ProjectPlanningFilterDto input)
    {
        await EnsureDefaultsAsync();

        var setting  = await LoadSettingAsync();
        var stages   = await _stages.GetListAsync();
        var teams    = await _teams.GetListAsync();
        var filtered = ApplyFilter(await _projects.GetListAsync(), input);
        var dtos     = await BuildProjectDtosAsync(filtered, stages, teams, setting);

        var finalIds = stages.Where(s => s.IsFinal).Select(s => s.Id).ToHashSet();
        var running  = dtos.Where(p => !finalIds.Contains(p.StageId)).ToList();

        var weekStart = WeekStart(Clock.Now);
        var weekEnd   = weekStart.AddDays(6);
        var projectIds = filtered.Select(p => p.Id).ToList();
        var weekEntries = await _entries.GetListAsync(e =>
            e.Date >= weekStart && e.Date < weekEnd && projectIds.Contains(e.ProjectId));

        var items = dtos
            .Where(p => NeedsAttention(p, stages))
            .OrderByDescending(p => p.WaitingDays)
            .ThenBy(p => p.Code)
            .ToList();

        var timePerStage = new List<ProjectStageTimeDto>();
        foreach (var s in stages.Where(s => s.IsActive).OrderBy(s => s.Order).ThenBy(s => s.Id))
        {
            var closed = await _history.GetListAsync(h => h.StageId == s.Id && h.LeftAt != null);
            var last   = closed.OrderByDescending(h => h.LeftAt).Take(20).ToList();
            timePerStage.Add(new ProjectStageTimeDto
            {
                StageId     = s.Id,
                Name        = s.Name,
                Color       = s.Color,
                TargetDays  = s.TargetDays,
                AverageDays = last.Count == 0
                    ? null
                    : Math.Round(last.Average(h => (h.LeftAt!.Value - h.EnteredAt).TotalDays), 1),
            });
        }

        return new ProjectAttentionDto
        {
            Running          = running.Count,
            RunningValue     = running.Sum(p => p.Value),
            Blocked          = running.Count(p => p.IsBlocked),
            Late             = running.Count(p => p.IsLate),
            InstallsThisWeek = weekEntries.Select(e => e.ProjectId).Distinct().Count(),
            InstallTeams     = weekEntries.Select(e => e.TeamId).Distinct().Count(),
            Items            = items,
            TimePerStage     = timePerStage,
            SeasonNotes      = (await _notes.GetListAsync()).OrderBy(n => n.Order).ThenBy(n => n.Id).Select(MapNote).ToList(),
        };
    }

    public async Task<ProjectWeekDto> GetWeekAsync(ProjectWeekFilterDto input)
    {
        await EnsureDefaultsAsync();

        var setting   = await LoadSettingAsync();
        var stages    = await _stages.GetListAsync();
        var teams     = await _teams.GetListAsync();
        var filtered  = ApplyFilter(await _projects.GetListAsync(), input);
        var dtos      = await BuildProjectDtosAsync(filtered, stages, teams, setting);
        var projectIds = filtered.Select(p => p.Id).ToList();

        var weekStart = WeekStart(input.Date ?? Clock.Now);
        var weekEnd   = weekStart.AddDays(6);

        var weekEntries = await _entries.GetListAsync(e =>
            e.Date >= weekStart && e.Date < weekEnd && projectIds.Contains(e.ProjectId));

        var byId   = dtos.ToDictionary(p => p.Id);
        var rowTeams = teams.Where(t => t.IsActive)
            .Where(t => !input.TeamId.HasValue || t.Id == input.TeamId.Value)
            .OrderBy(t => t.Order).ThenBy(t => t.Id)
            .ToList();

        var rows = rowTeams.Select(t => new ProjectWeekRowDto
        {
            Team    = MapTeam(t),
            Entries = weekEntries.Where(e => e.TeamId == t.Id)
                .OrderBy(e => e.Date)
                .Select(e =>
                {
                    var p = byId[e.ProjectId];
                    return new ProjectWeekEntryDto
                    {
                        Id        = e.Id,
                        ProjectId = e.ProjectId,
                        TeamId    = e.TeamId,
                        Date      = e.Date.Date,
                        Title     = string.IsNullOrWhiteSpace(p.Title) ? p.CustomerName : $"{p.CustomerName} · {p.Title}",
                        Note      = e.Note,
                        Color     = p.StageColor,
                        IsBlocked = p.IsBlocked,
                    };
                }).ToList(),
        }).ToList();

        var finalIds = stages.Where(s => s.IsFinal).Select(s => s.Id).ToHashSet();
        var schedulable = stages.Where(s => s.RequiresScheduling).Select(s => s.Id).ToHashSet();
        var weekProjectIds = weekEntries.Select(e => e.ProjectId).ToHashSet();

        var waiting = dtos
            .Where(p => schedulable.Contains(p.StageId) && !finalIds.Contains(p.StageId) && p.NextScheduleDate == null)
            .OrderByDescending(p => p.DaysInStage)
            .ToList();

        var materials = dtos
            .Where(p => weekProjectIds.Contains(p.Id) && p.MaterialStatus != ProjectMaterialStatus.None)
            .OrderBy(p => p.NextScheduleDate)
            .ToList();

        return new ProjectWeekDto
        {
            WeekStart = weekStart,
            WeekEnd   = weekEnd.AddDays(-1),
            Rows      = rows,
            Waiting   = waiting,
            Materials = materials,
        };
    }

    // ══ CUSTOMISATION ════════════════════════════════════════════════════════

    public async Task<ProjectPlanningSettingDto> GetSettingAsync()
    {
        await EnsureDefaultsAsync();
        return MapSetting(await LoadSettingAsync());
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectPlanningSettingDto> UpdateSettingAsync(ProjectPlanningSettingDto input)
    {
        await EnsureDefaultsAsync();
        var s = await LoadSettingAsync();

        s.AccentColor    = input.AccentColor;
        s.CurrencySymbol = input.CurrencySymbol;
        s.DueSoonDays    = input.DueSoonDays;
        s.PageTitle      = input.PageTitle;
        s.PageSubtitle   = input.PageSubtitle ?? string.Empty;
        s.NewProjectLabel  = input.NewProjectLabel  ?? string.Empty;
        s.BoardTabLabel    = input.BoardTabLabel    ?? string.Empty;
        s.WeekTabLabel     = input.WeekTabLabel     ?? string.Empty;
        s.AttentionTabLabel = input.AttentionTabLabel ?? string.Empty;
        s.StagesTabLabel   = input.StagesTabLabel   ?? string.Empty;
        s.StagesIntro      = input.StagesIntro      ?? string.Empty;
        s.AttentionTitle   = input.AttentionTitle   ?? string.Empty;
        s.ScheduleTitle    = input.ScheduleTitle    ?? string.Empty;
        s.TimeTitle        = input.TimeTitle        ?? string.Empty;
        s.TimeFootnote     = input.TimeFootnote     ?? string.Empty;
        s.SeasonTitle      = input.SeasonTitle      ?? string.Empty;
        s.SeasonSubtitle   = input.SeasonSubtitle   ?? string.Empty;

        await _settings.UpdateAsync(s, autoSave: true);
        return MapSetting(s);
    }

    public async Task<List<ProjectStageDto>> GetStagesAsync()
    {
        await EnsureDefaultsAsync();
        return (await _stages.GetListAsync())
            .OrderBy(s => s.Order).ThenBy(s => s.Id)
            .Select(MapStage).ToList();
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectStageDto> CreateStageAsync(CreateUpdateProjectStageDto input)
    {
        var stage = new ProjectStage();
        ApplyStage(stage, input);
        if (input.Order <= 0)
            stage.Order = (await _stages.GetListAsync()).Select(s => s.Order).DefaultIfEmpty(0).Max() + 1;

        await _stages.InsertAsync(stage, autoSave: true);
        return MapStage(stage);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectStageDto> UpdateStageAsync(int id, CreateUpdateProjectStageDto input)
    {
        var stage = await _stages.GetAsync(id);
        ApplyStage(stage, input);
        await _stages.UpdateAsync(stage, autoSave: true);
        return MapStage(stage);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Delete)]
    public async Task DeleteStageAsync(int id)
    {
        if (await _projects.AnyAsync(p => p.StageId == id))
            throw new UserFriendlyException("This stage still has projects. Move them first, or switch the stage off instead.");

        await _stages.DeleteAsync(id, autoSave: true);
    }

    public async Task<List<ProjectTeamDto>> GetTeamsAsync()
    {
        await EnsureDefaultsAsync();
        return (await _teams.GetListAsync())
            .OrderBy(t => t.Order).ThenBy(t => t.Id)
            .Select(MapTeam).ToList();
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectTeamDto> CreateTeamAsync(CreateUpdateProjectTeamDto input)
    {
        var team = new ProjectTeam();
        ApplyTeam(team, input);
        if (input.Order <= 0)
            team.Order = (await _teams.GetListAsync()).Select(t => t.Order).DefaultIfEmpty(0).Max() + 1;

        await _teams.InsertAsync(team, autoSave: true);
        return MapTeam(team);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectTeamDto> UpdateTeamAsync(int id, CreateUpdateProjectTeamDto input)
    {
        var team = await _teams.GetAsync(id);
        ApplyTeam(team, input);
        await _teams.UpdateAsync(team, autoSave: true);
        return MapTeam(team);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Delete)]
    public async Task DeleteTeamAsync(int id)
    {
        if (await _projects.AnyAsync(p => p.TeamId == id) || await _entries.AnyAsync(e => e.TeamId == id))
            throw new UserFriendlyException("This team is used by projects or the schedule. Switch it off instead of deleting it.");

        await _teams.DeleteAsync(id, autoSave: true);
    }

    public async Task<List<ProjectSeasonNoteDto>> GetSeasonNotesAsync()
    {
        await EnsureDefaultsAsync();
        return (await _notes.GetListAsync())
            .OrderBy(n => n.Order).ThenBy(n => n.Id)
            .Select(MapNote).ToList();
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectSeasonNoteDto> CreateSeasonNoteAsync(CreateUpdateProjectSeasonNoteDto input)
    {
        var note = new ProjectSeasonNote();
        ApplyNote(note, input);
        if (input.Order <= 0)
            note.Order = (await _notes.GetListAsync()).Select(n => n.Order).DefaultIfEmpty(0).Max() + 1;

        await _notes.InsertAsync(note, autoSave: true);
        return MapNote(note);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectSeasonNoteDto> UpdateSeasonNoteAsync(int id, CreateUpdateProjectSeasonNoteDto input)
    {
        var note = await _notes.GetAsync(id);
        ApplyNote(note, input);
        await _notes.UpdateAsync(note, autoSave: true);
        return MapNote(note);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Delete)]
    public async Task DeleteSeasonNoteAsync(int id)
    {
        await _notes.DeleteAsync(id, autoSave: true);
    }

    // ══ PROJECTS ═════════════════════════════════════════════════════════════

    public async Task<ProjectDto> GetProjectAsync(int id)
    {
        return await LoadProjectDtoAsync(await _projects.GetAsync(id));
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Create)]
    public async Task<ProjectDto> CreateProjectAsync(CreateUpdateProjectDto input)
    {
        await EnsureDefaultsAsync();

        var stages = await _stages.GetListAsync(s => s.IsActive);
        var stage  = input.StageId.HasValue
            ? stages.FirstOrDefault(s => s.Id == input.StageId.Value)
            : stages.OrderBy(s => s.Order).ThenBy(s => s.Id).FirstOrDefault();
        if (stage == null)
            throw new UserFriendlyException("Pick a valid stage for the project.");

        var now = Clock.Now;
        var project = new Project
        {
            StageId        = stage.Id,
            StageEnteredAt = now,
            Code           = "PENDING",
        };
        ApplyProject(project, input);

        await _projects.InsertAsync(project, autoSave: true);
        project.Code = $"PRJ-{project.Id:D4}";
        await _projects.UpdateAsync(project, autoSave: true);

        await _history.InsertAsync(new ProjectStageHistory
        {
            ProjectId = project.Id,
            StageId   = stage.Id,
            EnteredAt = now,
        }, autoSave: true);

        return await LoadProjectDtoAsync(project);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectDto> UpdateProjectAsync(int id, CreateUpdateProjectDto input)
    {
        var project = await _projects.GetAsync(id);
        ApplyProject(project, input);
        await _projects.UpdateAsync(project, autoSave: true);
        return await LoadProjectDtoAsync(project);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Delete)]
    public async Task DeleteProjectAsync(int id)
    {
        await _entries.DeleteAsync(e => e.ProjectId == id, autoSave: true);
        await _projects.DeleteAsync(id, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectDto> UpdateProjectStageAsync(int id, MoveProjectStageDto input)
    {
        var project = await _projects.GetAsync(id);
        if (project.StageId == input.StageId)
            return await LoadProjectDtoAsync(project);

        var stages  = await _stages.GetListAsync();
        var current = stages.FirstOrDefault(s => s.Id == project.StageId);
        var target  = stages.FirstOrDefault(s => s.Id == input.StageId && s.IsActive)
                      ?? throw new UserFriendlyException("That stage does not exist or is switched off.");

        // Moving forward requires every tick of the current stage (what keeps jobs from getting stuck).
        if (!input.Force && current != null && target.Order > current.Order)
        {
            var done = ParseLines(project.ChecklistDone);
            var missing = ParseLines(current.ChecklistText)
                .Where(item => !done.Contains(ChecklistKey(current.Id, item)))
                .ToList();
            if (missing.Count > 0)
            {
                throw new BusinessException(ChecklistIncompleteCode,
                        $"“{current.Name}” still has {missing.Count} unticked checklist item(s).")
                    .WithData("Missing", string.Join(" | ", missing));
            }
        }

        var now = Clock.Now;
        var open = await _history.GetListAsync(h => h.ProjectId == id && h.LeftAt == null);
        foreach (var h in open)
        {
            h.LeftAt = now;
            await _history.UpdateAsync(h);
        }
        await _history.InsertAsync(new ProjectStageHistory
        {
            ProjectId = id,
            StageId   = target.Id,
            EnteredAt = now,
        });

        project.StageId        = target.Id;
        project.StageEnteredAt = now;
        project.IsBlocked      = false;
        project.BlockedReason  = null;
        project.WaitingFor     = null;
        project.WaitingSince   = null;
        project.ActionLabel    = null;
        await _projects.UpdateAsync(project, autoSave: true);

        return await LoadProjectDtoAsync(project);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task<ProjectDto> UpdateProjectMaterialAsync(int id, UpdateProjectMaterialDto input)
    {
        var project = await _projects.GetAsync(id);
        project.MaterialStatus = input.Status;
        project.MaterialNote   = Clean(input.Note);
        await _projects.UpdateAsync(project, autoSave: true);
        return await LoadProjectDtoAsync(project);
    }

    // ══ CREW SCHEDULE ════════════════════════════════════════════════════════

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task CreateScheduleEntryAsync(CreateUpdateProjectScheduleEntryDto input)
    {
        await EnsureScheduleTargetsAsync(input);
        await _entries.InsertAsync(new ProjectScheduleEntry
        {
            ProjectId = input.ProjectId,
            TeamId    = input.TeamId,
            Date      = input.Date.Date,
            Note      = Clean(input.Note),
        }, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task UpdateScheduleEntryAsync(int id, CreateUpdateProjectScheduleEntryDto input)
    {
        await EnsureScheduleTargetsAsync(input);
        var entry = await _entries.GetAsync(id);
        entry.ProjectId = input.ProjectId;
        entry.TeamId    = input.TeamId;
        entry.Date      = input.Date.Date;
        entry.Note      = Clean(input.Note);
        await _entries.UpdateAsync(entry, autoSave: true);
    }

    [Authorize(DymoEnergyPermissions.ProjectPlanning.Edit)]
    public async Task DeleteScheduleEntryAsync(int id)
    {
        await _entries.DeleteAsync(id, autoSave: true);
    }

    // ══ HELPERS ══════════════════════════════════════════════════════════════

    private async Task EnsureScheduleTargetsAsync(CreateUpdateProjectScheduleEntryDto input)
    {
        if (!await _projects.AnyAsync(p => p.Id == input.ProjectId))
            throw new UserFriendlyException("Project not found.");
        if (!await _teams.AnyAsync(t => t.Id == input.TeamId))
            throw new UserFriendlyException("Team not found.");
    }

    private async Task<ProjectPlanningSetting> LoadSettingAsync()
    {
        var list = await _settings.GetListAsync();
        return list.OrderBy(s => s.Id).First();
    }

    private async Task<ProjectDto> LoadProjectDtoAsync(Project project)
    {
        var setting = await LoadSettingAsync();
        var stages  = await _stages.GetListAsync();
        var teams   = await _teams.GetListAsync();
        return (await BuildProjectDtosAsync(new List<Project> { project }, stages, teams, setting)).First();
    }

    private static List<Project> ApplyFilter(List<Project> projects, ProjectPlanningFilterDto? input)
    {
        IEnumerable<Project> q = projects;
        if (input?.TeamId != null)
            q = q.Where(p => p.TeamId == input.TeamId);
        if (!string.IsNullOrWhiteSpace(input?.District))
            q = q.Where(p => string.Equals(p.District, input!.District, StringComparison.OrdinalIgnoreCase));
        return q.OrderBy(p => p.Id).ToList();
    }

    private async Task<List<ProjectDto>> BuildProjectDtosAsync(
        List<Project> projects, List<ProjectStage> stages, List<ProjectTeam> teams, ProjectPlanningSetting setting)
    {
        var today = Clock.Now.Date;
        var ids   = projects.Select(p => p.Id).ToList();

        var future = ids.Count == 0
            ? new List<ProjectScheduleEntry>()
            : await _entries.GetListAsync(e => ids.Contains(e.ProjectId) && e.Date >= today);
        var nextByProject = future
            .GroupBy(e => e.ProjectId)
            .ToDictionary(g => g.Key, g => g.Min(e => e.Date));

        var stageById = stages.ToDictionary(s => s.Id);
        var teamById  = teams.ToDictionary(t => t.Id);

        return projects.Select(p =>
        {
            stageById.TryGetValue(p.StageId, out var stage);
            ProjectTeam? team = null;
            if (p.TeamId.HasValue) teamById.TryGetValue(p.TeamId.Value, out team);

            var isFinal = stage?.IsFinal == true;
            var due     = p.DueDate?.Date;
            var late    = due.HasValue && due.Value < today && !isFinal;
            var soon    = due.HasValue && !late && !isFinal && due.Value <= today.AddDays(setting.DueSoonDays);

            var waitingDays = 0;
            if (p.IsBlocked)
                waitingDays = Math.Max(0, (today - (p.WaitingSince ?? p.StageEnteredAt).Date).Days);
            else if (late)
                waitingDays = (today - due!.Value).Days;

            return new ProjectDto
            {
                Id            = p.Id,
                Code          = p.Code,
                CustomerName  = p.CustomerName,
                Title         = p.Title,
                District      = p.District,
                Value         = p.Value,
                StageId       = p.StageId,
                StageName     = stage?.Name ?? string.Empty,
                StageColor    = stage?.Color ?? "#6B7280",
                DaysInStage   = Math.Max(0, (today - p.StageEnteredAt.Date).Days),
                TeamId        = p.TeamId,
                TeamName      = team?.Name,
                TeamColor     = team?.Color,
                DueDate       = p.DueDate,
                NextScheduleDate = nextByProject.TryGetValue(p.Id, out var next) ? next : null,
                Tags          = SplitTags(p.Tags),
                StatusNote    = p.StatusNote,
                Notes         = p.Notes,
                IsBlocked     = p.IsBlocked,
                BlockedReason = p.BlockedReason,
                WaitingFor    = p.WaitingFor,
                WaitingSince  = p.WaitingSince,
                ActionLabel   = p.ActionLabel,
                WaitingDays   = waitingDays,
                IsLate        = late,
                IsDueSoon     = soon,
                MaterialStatus = p.MaterialStatus,
                MaterialNote  = p.MaterialNote,
                ChecklistDone = ParseLines(p.ChecklistDone).ToList(),
            };
        }).ToList();
    }

    private static bool NeedsAttention(ProjectDto p, List<ProjectStage> stages)
    {
        var stage = stages.FirstOrDefault(s => s.Id == p.StageId);
        return stage?.IsFinal != true && (p.IsBlocked || p.IsLate);
    }

    /// <summary>
    /// Weeks run Saturday → Thursday on the grid. Friday is the day off, so it belongs to the
    /// upcoming week — that is the week people are planning on a Friday.
    /// </summary>
    private static DateTime WeekStart(DateTime date)
    {
        var d = date.Date;
        if (d.DayOfWeek == DayOfWeek.Friday) d = d.AddDays(1);
        return d.AddDays(-(((int)d.DayOfWeek + 1) % 7));
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string ChecklistKey(int stageId, string title) => $"{stageId}|{title}";

    private static List<string> ParseLines(string? text) =>
        (text ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static string? JoinLines(IEnumerable<string>? items)
    {
        var lines = (items ?? Enumerable.Empty<string>())
            .Select(i => i.Replace("\r", " ").Replace("\n", " ").Trim())
            .Where(i => i.Length > 0)
            .ToList();
        return lines.Count == 0 ? null : string.Join('\n', lines);
    }

    private static List<string> SplitTags(string? tags) =>
        (tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static void ApplyStage(ProjectStage s, CreateUpdateProjectStageDto i)
    {
        s.Name               = i.Name.Trim();
        s.Color              = i.Color;
        s.Responsible        = Clean(i.Responsible);
        s.DurationText       = Clean(i.DurationText);
        s.ProducesText       = Clean(i.ProducesText);
        s.TargetDays         = i.TargetDays;
        s.RequiresScheduling = i.RequiresScheduling;
        s.IsFinal            = i.IsFinal;
        s.Order              = i.Order;
        s.IsActive           = i.IsActive;
        s.ChecklistText      = JoinLines(i.Checklist);
    }

    private static void ApplyTeam(ProjectTeam t, CreateUpdateProjectTeamDto i)
    {
        t.Name        = i.Name.Trim();
        t.Description = Clean(i.Description);
        t.Color       = i.Color;
        t.Order       = i.Order;
        t.IsActive    = i.IsActive;
    }

    private static void ApplyNote(ProjectSeasonNote n, CreateUpdateProjectSeasonNoteDto i)
    {
        n.Title       = i.Title.Trim();
        n.Period      = Clean(i.Period);
        n.Description = Clean(i.Description);
        n.Color       = i.Color;
        n.Order       = i.Order;
    }

    private static void ApplyProject(Project p, CreateUpdateProjectDto i)
    {
        p.CustomerName  = i.CustomerName.Trim();
        p.Title         = Clean(i.Title);
        p.District      = Clean(i.District);
        p.Value         = i.Value;
        p.TeamId        = i.TeamId;
        p.DueDate       = i.DueDate?.Date;
        p.Tags          = i.Tags.Count == 0
            ? null
            : string.Join(", ", i.Tags.Select(t => t.Replace(",", " ").Trim()).Where(t => t.Length > 0));
        p.StatusNote    = Clean(i.StatusNote);
        p.Notes         = Clean(i.Notes);
        p.IsBlocked     = i.IsBlocked;
        p.BlockedReason = i.IsBlocked ? Clean(i.BlockedReason) : null;
        p.WaitingFor    = i.IsBlocked ? Clean(i.WaitingFor) : null;
        p.WaitingSince  = i.IsBlocked ? (i.WaitingSince ?? p.WaitingSince ?? DateTime.UtcNow) : null;
        p.ActionLabel   = i.IsBlocked ? Clean(i.ActionLabel) : null;
        p.MaterialStatus = i.MaterialStatus;
        p.MaterialNote  = Clean(i.MaterialNote);
        p.ChecklistDone = JoinLines(i.ChecklistDone);
    }

    private static ProjectPlanningSettingDto MapSetting(ProjectPlanningSetting s) => new()
    {
        AccentColor       = s.AccentColor,
        CurrencySymbol    = s.CurrencySymbol,
        DueSoonDays       = s.DueSoonDays,
        PageTitle         = s.PageTitle,
        PageSubtitle      = s.PageSubtitle,
        NewProjectLabel   = s.NewProjectLabel,
        BoardTabLabel     = s.BoardTabLabel,
        WeekTabLabel      = s.WeekTabLabel,
        AttentionTabLabel = s.AttentionTabLabel,
        StagesTabLabel    = s.StagesTabLabel,
        StagesIntro       = s.StagesIntro,
        AttentionTitle    = s.AttentionTitle,
        ScheduleTitle     = s.ScheduleTitle,
        TimeTitle         = s.TimeTitle,
        TimeFootnote      = s.TimeFootnote,
        SeasonTitle       = s.SeasonTitle,
        SeasonSubtitle    = s.SeasonSubtitle,
    };

    private static ProjectStageDto MapStage(ProjectStage s) => new()
    {
        Id                 = s.Id,
        Name               = s.Name,
        Color              = s.Color,
        Responsible        = s.Responsible,
        DurationText       = s.DurationText,
        ProducesText       = s.ProducesText,
        TargetDays         = s.TargetDays,
        RequiresScheduling = s.RequiresScheduling,
        IsFinal            = s.IsFinal,
        Order              = s.Order,
        IsActive           = s.IsActive,
        Checklist          = ParseLines(s.ChecklistText),
    };

    private static ProjectTeamDto MapTeam(ProjectTeam t) => new()
    {
        Id = t.Id, Name = t.Name, Description = t.Description, Color = t.Color, Order = t.Order, IsActive = t.IsActive,
    };

    private static ProjectSeasonNoteDto MapNote(ProjectSeasonNote n) => new()
    {
        Id = n.Id, Title = n.Title, Period = n.Period, Description = n.Description, Color = n.Color, Order = n.Order,
    };

    // ══ DEFAULTS ═════════════════════════════════════════════════════════════

    /// <summary>
    /// Seeds the editable defaults (stages, teams, season notes, labels) the first time the page is opened,
    /// so the admin has something to customise instead of an empty screen.
    /// </summary>
    private async Task EnsureDefaultsAsync()
    {
        if (await _settings.GetCountAsync() > 0) return;

        await SeedLock.WaitAsync();
        try
        {
            if (await _settings.GetCountAsync() > 0) return;

            var firstRun = await _stages.GetCountAsync() == 0;

            await _settings.InsertAsync(new ProjectPlanningSetting
            {
                TimeFootnote = "Net metering and the meter visit are the two long waits — start them the day the quote is accepted, not after installation.",
            }, autoSave: true);

            if (!firstRun) return;

            var stages = new[]
            {
                NewStage(1, "Site survey",       "#0E6B3F", "Surveyor",              "1–3 days",  "Survey report with photos",                                 3,  true,  false,
                    "Measure the roof and shade, note the angle", "Photograph the meter, main switch and cable route",
                    "Check the roof can take the weight", "Record the monthly bill and load"),
                NewStage(2, "Design & quote",    "#0E6B3F", "Sales + engineer",      "2–5 days",  "Signed quote + advance receipt",                            5,  false, false,
                    "Pick panel, inverter and battery sizes", "Draw the layout and single-line diagram",
                    "Send the quote; get the advance", "Convert the accepted quote into an order"),
                NewStage(3, "Net metering",      "#2563EB", "Engineer",              "2–6 weeks", "Approval letter — installation must follow the approved design", 14, false, false,
                    "Prepare the drawings the utility asks for", "Apply online with the customer's meter account number",
                    "Utility checks the application (about 5 working days)", "Utility visits the site (about 10 working days)"),
                NewStage(4, "Materials",         "#D97706", "Store manager",         "3–7 days",  "Packing list tied to the job",                              7,  false, false,
                    "Reserve panels, inverter, battery and cable from stock", "Order anything short, with the delivery date",
                    "Pack per job, with serial numbers recorded", "Arrange transport to the site"),
                NewStage(5, "Installation",      "#0E6B3F", "Installation team",     "1–3 days",  "Completion photos + job sheet",                             3,  true,  false,
                    "Mount the rails and panels, keep the roof watertight", "Fit the inverter and battery, run DC and AC cable",
                    "Earthing and surge protection", "Test, photograph each step, customer signs the job sheet"),
                NewStage(6, "Inspection & meter", "#2563EB", "Engineer + utility",   "1–3 weeks", "Net-metering agreement + new meter",                        10, true,  false,
                    "Our own check: earthing, torque, labels, safety signs", "Utility checks the installed system against the approved design",
                    "Sign the agreement; bidirectional meter fitted (about 15 working days)", "Fix any snag the inspector lists"),
                NewStage(7, "Handover",          "#6B7280", "Engineer",              "1–2 days",  "Handover sheet + warranty pack",                            2,  false, true,
                    "Show the customer the app and the switches", "Hand over warranty cards and serial list",
                    "Book the first service visit", "Collect the balance; ask for a referral"),
            };
            await _stages.InsertManyAsync(stages, autoSave: true);

            await _teams.InsertManyAsync(new[]
            {
                new ProjectTeam { Name = "Team A",   Description = "2 technicians", Color = "#0E6B3F", Order = 1 },
                new ProjectTeam { Name = "Team B",   Description = "2 technicians", Color = "#2563EB", Order = 2 },
                new ProjectTeam { Name = "Surveyor", Description = "1 person · all districts", Color = "#D97706", Order = 3 },
            }, autoSave: true);

            await _notes.InsertManyAsync(new[]
            {
                new ProjectSeasonNote { Order = 1, Color = "#F29D12", Title = "Load-shedding",  Period = "Apr – Jun",
                    Description = "Backup and IPS jobs double. Book extra crew and keep battery stock high." },
                new ProjectSeasonNote { Order = 2, Color = "#2563EB", Title = "Monsoon",        Period = "Jun – Sep",
                    Description = "Avoid roof work on rain days. Keep 2 indoor jobs (inverter swaps, service visits) as backup." },
                new ProjectSeasonNote { Order = 3, Color = "#0E6B3F", Title = "Boro irrigation", Period = "Dec – Feb",
                    Description = "Pump jobs must finish before the season starts. Survey farms in November." },
                new ProjectSeasonNote { Order = 4, Color = "#6B7280", Title = "Ramadan & Eid",  Period = null,
                    Description = "Short working days; no installs on Eid week. Plan deliveries before." },
            }, autoSave: true);
        }
        finally
        {
            SeedLock.Release();
        }
    }

    private static ProjectStage NewStage(
        int order, string name, string color, string responsible, string duration, string produces,
        int targetDays, bool requiresScheduling, bool isFinal, params string[] checklist) => new()
    {
        Order              = order,
        Name               = name,
        Color              = color,
        Responsible        = responsible,
        DurationText       = duration,
        ProducesText       = produces,
        TargetDays         = targetDays,
        RequiresScheduling = requiresScheduling,
        IsFinal            = isFinal,
        ChecklistText      = string.Join('\n', checklist),
    };
}

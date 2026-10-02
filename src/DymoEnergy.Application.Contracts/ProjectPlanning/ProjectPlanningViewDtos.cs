using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.ProjectPlanning;

public class ProjectStageSummaryDto
{
    public ProjectStageDto Stage { get; set; } = new();
    public int    Count      { get; set; }
    public double TotalValue { get; set; }
}

public class ProjectBoardDto
{
    public ProjectPlanningSettingDto Setting { get; set; } = new();
    public List<ProjectStageSummaryDto> Stages    { get; set; } = new();
    public List<ProjectDto>             Projects  { get; set; } = new();
    public List<ProjectTeamDto>         Teams     { get; set; } = new();
    public List<string>                 Districts { get; set; } = new();
    public int AttentionCount { get; set; }
}

public class ProjectStageTimeDto
{
    public int     StageId     { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string  Color       { get; set; } = string.Empty;
    public double? AverageDays { get; set; }
    public int?    TargetDays  { get; set; }
}

public class ProjectAttentionDto
{
    public int    Running      { get; set; }
    public double RunningValue { get; set; }
    public int    Blocked      { get; set; }
    public int    Late         { get; set; }
    public int    InstallsThisWeek { get; set; }
    public int    InstallTeams     { get; set; }
    public List<ProjectDto>           Items        { get; set; } = new();
    public List<ProjectStageTimeDto>  TimePerStage { get; set; } = new();
    public List<ProjectSeasonNoteDto> SeasonNotes  { get; set; } = new();
}

public class ProjectWeekEntryDto
{
    public int      Id        { get; set; }
    public int      ProjectId { get; set; }
    public int      TeamId    { get; set; }
    public DateTime Date      { get; set; }
    public string   Title     { get; set; } = string.Empty;
    public string?  Note      { get; set; }
    public string   Color     { get; set; } = string.Empty;
    public bool     IsBlocked { get; set; }
}

public class ProjectWeekRowDto
{
    public ProjectTeamDto Team { get; set; } = new();
    public List<ProjectWeekEntryDto> Entries { get; set; } = new();
}

public class ProjectWeekDto
{
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd   { get; set; }
    public List<ProjectWeekRowDto> Rows      { get; set; } = new();
    public List<ProjectDto>        Waiting   { get; set; } = new();
    public List<ProjectDto>        Materials { get; set; } = new();
}

public class ProjectWeekFilterDto : ProjectPlanningFilterDto
{
    /// <summary>Any day of the wanted week; the week starts on Saturday. Defaults to today.</summary>
    public DateTime? Date { get; set; }
}

public class CreateUpdateProjectScheduleEntryDto
{
    public int      ProjectId { get; set; }
    public int      TeamId    { get; set; }
    public DateTime Date      { get; set; }
    [MaxLength(256)] public string? Note { get; set; }
}

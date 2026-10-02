using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.ProjectPlanning;

public class ProjectDto
{
    public int     Id           { get; set; }
    public string  Code         { get; set; } = string.Empty;
    public string  CustomerName { get; set; } = string.Empty;
    public string? Title        { get; set; }
    public string? District     { get; set; }
    public double  Value        { get; set; }

    public int      StageId       { get; set; }
    public string   StageName     { get; set; } = string.Empty;
    public string   StageColor    { get; set; } = string.Empty;
    public int      DaysInStage   { get; set; }
    public int?     TeamId        { get; set; }
    public string?  TeamName      { get; set; }
    public string?  TeamColor     { get; set; }
    public DateTime? DueDate      { get; set; }
    public DateTime? NextScheduleDate { get; set; }

    public List<string> Tags { get; set; } = new();
    public string? StatusNote { get; set; }
    public string? Notes      { get; set; }

    public bool      IsBlocked     { get; set; }
    public string?   BlockedReason { get; set; }
    public string?   WaitingFor    { get; set; }
    public DateTime? WaitingSince  { get; set; }
    public string?   ActionLabel   { get; set; }
    public int       WaitingDays   { get; set; }
    public bool      IsLate        { get; set; }
    public bool      IsDueSoon     { get; set; }

    public ProjectMaterialStatus MaterialStatus { get; set; }
    public string?               MaterialNote   { get; set; }

    /// <summary>Keys ("stageId|title") of the ticked checklist items.</summary>
    public List<string> ChecklistDone { get; set; } = new();
}

public class CreateUpdateProjectDto
{
    [Required, MaxLength(256)] public string CustomerName { get; set; } = string.Empty;
    [MaxLength(256)] public string? Title    { get; set; }
    [MaxLength(128)] public string? District { get; set; }
    [Range(0, double.MaxValue)] public double Value { get; set; }

    /// <summary>Required on create; ignored on update (use UpdateProjectStage to move a job).</summary>
    public int?      StageId { get; set; }
    public int?      TeamId  { get; set; }
    public DateTime? DueDate { get; set; }

    public List<string> Tags { get; set; } = new();
    [MaxLength(256)]  public string? StatusNote { get; set; }
    [MaxLength(2000)] public string? Notes      { get; set; }

    public bool IsBlocked { get; set; }
    [MaxLength(256)] public string? BlockedReason { get; set; }
    [MaxLength(256)] public string? WaitingFor    { get; set; }
    public DateTime? WaitingSince { get; set; }
    [MaxLength(128)] public string? ActionLabel   { get; set; }

    public ProjectMaterialStatus MaterialStatus { get; set; }
    [MaxLength(256)] public string? MaterialNote { get; set; }

    public List<string> ChecklistDone { get; set; } = new();
}

public class MoveProjectStageDto
{
    public int  StageId { get; set; }
    /// <summary>Skip the "checklist must be complete" rule.</summary>
    public bool Force   { get; set; }
}

public class UpdateProjectMaterialDto
{
    public ProjectMaterialStatus Status { get; set; }
    [MaxLength(256)] public string? Note { get; set; }
}

public class ProjectPlanningFilterDto
{
    public int?    TeamId   { get; set; }
    public string? District { get; set; }
}

using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.ProjectPlanning;

public class Project : FullAuditedAggregateRoot<int>
{
    public string  Code         { get; set; } = string.Empty;
    public string  CustomerName { get; set; } = string.Empty;
    /// <summary>What is being built, e.g. "3 kW on-grid".</summary>
    public string? Title        { get; set; }
    public string? District     { get; set; }
    public double  Value        { get; set; }

    public int     StageId        { get; set; }
    public DateTime StageEnteredAt { get; set; }
    public int?    TeamId       { get; set; }
    public DateTime? DueDate    { get; set; }

    /// <summary>Comma separated chips shown on the board card.</summary>
    public string? Tags         { get; set; }
    /// <summary>Footer line of the board card, e.g. "Survey 4 Oct".</summary>
    public string? StatusNote   { get; set; }
    public string? Notes        { get; set; }

    // ── Needs attention ───────────────────────────────────────────────────
    public bool      IsBlocked     { get; set; }
    public string?   BlockedReason { get; set; }
    public string?   WaitingFor    { get; set; }
    public DateTime? WaitingSince  { get; set; }
    public string?   ActionLabel   { get; set; }

    // ── Materials ─────────────────────────────────────────────────────────
    public ProjectMaterialStatus MaterialStatus { get; set; } = ProjectMaterialStatus.None;
    public string?               MaterialNote   { get; set; }

    /// <summary>Ticked checklist items as "stageId|title" lines.</summary>
    public string? ChecklistDone { get; set; }
}

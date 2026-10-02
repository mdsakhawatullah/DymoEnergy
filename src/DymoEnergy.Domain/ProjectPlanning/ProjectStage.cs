using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.ProjectPlanning;

/// <summary>One configurable step of an installation job (Site survey, Net metering, ...).</summary>
public class ProjectStage : FullAuditedAggregateRoot<int>
{
    public string  Name          { get; set; } = string.Empty;
    /// <summary>Hex colour (#RRGGBB) used for the stage dot, bar and chips.</summary>
    public string  Color         { get; set; } = "#0E6B3F";
    public string? Responsible   { get; set; }
    /// <summary>Free text shown on the stage card, e.g. "1–3 days".</summary>
    public string? DurationText  { get; set; }
    public string? ProducesText  { get; set; }
    /// <summary>Expected days; used to colour the "Where time goes" bars.</summary>
    public int?    TargetDays    { get; set; }
    /// <summary>Jobs in this stage show up in "Waiting to be scheduled" until a crew is booked.</summary>
    public bool    RequiresScheduling { get; set; }
    /// <summary>Last stage (handover): jobs here are finished and never counted as running.</summary>
    public bool    IsFinal       { get; set; }
    public int     Order         { get; set; }
    public bool    IsActive      { get; set; } = true;
    /// <summary>Checklist template, one item per line.</summary>
    public string? ChecklistText { get; set; }
}

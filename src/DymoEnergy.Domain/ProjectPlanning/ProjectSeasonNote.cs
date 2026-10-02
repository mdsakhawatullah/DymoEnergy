using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.ProjectPlanning;

public class ProjectSeasonNote : FullAuditedAggregateRoot<int>
{
    public string  Title       { get; set; } = string.Empty;
    /// <summary>Free text period, e.g. "Apr – Jun".</summary>
    public string? Period      { get; set; }
    public string? Description { get; set; }
    public string  Color       { get; set; } = "#F29D12";
    public int     Order       { get; set; }
}

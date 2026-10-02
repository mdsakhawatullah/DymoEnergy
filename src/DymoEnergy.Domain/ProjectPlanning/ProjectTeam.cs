using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.ProjectPlanning;

/// <summary>A row of the crew schedule: install team, contractor, surveyor...</summary>
public class ProjectTeam : FullAuditedAggregateRoot<int>
{
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string  Color       { get; set; } = "#0E6B3F";
    public int     Order       { get; set; }
    public bool    IsActive    { get; set; } = true;
}

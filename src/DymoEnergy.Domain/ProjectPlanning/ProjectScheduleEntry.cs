using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.ProjectPlanning;

public class ProjectScheduleEntry : FullAuditedAggregateRoot<int>
{
    public int      ProjectId { get; set; }
    public int      TeamId    { get; set; }
    /// <summary>Calendar day (time part ignored).</summary>
    public DateTime Date      { get; set; }
    public string?  Note      { get; set; }
}

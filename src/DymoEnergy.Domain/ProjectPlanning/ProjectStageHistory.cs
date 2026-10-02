using System;
using Volo.Abp.Domain.Entities;

namespace DymoEnergy.ProjectPlanning;

/// <summary>Time a project spent in a stage; feeds the "Where time goes" chart.</summary>
public class ProjectStageHistory : AggregateRoot<int>
{
    public int       ProjectId { get; set; }
    public int       StageId   { get; set; }
    public DateTime  EnteredAt { get; set; }
    public DateTime? LeftAt    { get; set; }
}

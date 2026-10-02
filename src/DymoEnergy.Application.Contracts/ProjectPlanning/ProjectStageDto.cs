using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.ProjectPlanning;

public class ProjectStageDto
{
    public int     Id            { get; set; }
    public string  Name          { get; set; } = string.Empty;
    public string  Color         { get; set; } = "#0E6B3F";
    public string? Responsible   { get; set; }
    public string? DurationText  { get; set; }
    public string? ProducesText  { get; set; }
    public int?    TargetDays    { get; set; }
    public bool    RequiresScheduling { get; set; }
    public bool    IsFinal       { get; set; }
    public int     Order         { get; set; }
    public bool    IsActive      { get; set; } = true;
    public List<string> Checklist { get; set; } = new();
}

public class CreateUpdateProjectStageDto
{
    [Required, MaxLength(128)] public string Name  { get; set; } = string.Empty;
    [Required, MaxLength(16)]  public string Color { get; set; } = "#0E6B3F";
    [MaxLength(256)] public string? Responsible  { get; set; }
    [MaxLength(128)] public string? DurationText { get; set; }
    [MaxLength(256)] public string? ProducesText { get; set; }
    [Range(0, 3650)] public int?    TargetDays   { get; set; }
    public bool RequiresScheduling { get; set; }
    public bool IsFinal            { get; set; }
    public int  Order              { get; set; }
    public bool IsActive           { get; set; } = true;
    public List<string> Checklist  { get; set; } = new();
}

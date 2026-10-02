using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.ProjectPlanning;

public class ProjectTeamDto
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string  Color       { get; set; } = "#0E6B3F";
    public int     Order       { get; set; }
    public bool    IsActive    { get; set; } = true;
}

public class CreateUpdateProjectTeamDto
{
    [Required, MaxLength(128)] public string Name  { get; set; } = string.Empty;
    [MaxLength(256)]           public string? Description { get; set; }
    [Required, MaxLength(16)]  public string Color { get; set; } = "#0E6B3F";
    public int  Order    { get; set; }
    public bool IsActive { get; set; } = true;
}

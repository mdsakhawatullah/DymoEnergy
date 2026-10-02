using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.ProjectPlanning;

public class ProjectSeasonNoteDto
{
    public int     Id          { get; set; }
    public string  Title       { get; set; } = string.Empty;
    public string? Period      { get; set; }
    public string? Description { get; set; }
    public string  Color       { get; set; } = "#F29D12";
    public int     Order       { get; set; }
}

public class CreateUpdateProjectSeasonNoteDto
{
    [Required, MaxLength(256)] public string Title { get; set; } = string.Empty;
    [MaxLength(128)]  public string? Period      { get; set; }
    [MaxLength(2000)] public string? Description { get; set; }
    [Required, MaxLength(16)] public string Color { get; set; } = "#F29D12";
    public int Order { get; set; }
}

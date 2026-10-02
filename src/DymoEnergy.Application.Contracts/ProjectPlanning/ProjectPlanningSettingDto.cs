using System.ComponentModel.DataAnnotations;

namespace DymoEnergy.ProjectPlanning;

public class ProjectPlanningSettingDto
{
    [Required, MaxLength(16)]  public string AccentColor    { get; set; } = "#0E6B3F";
    [Required, MaxLength(8)]   public string CurrencySymbol { get; set; } = "৳";
    [Range(0, 60)]             public int    DueSoonDays    { get; set; } = 3;

    [Required, MaxLength(256)] public string  PageTitle       { get; set; } = string.Empty;
    [MaxLength(2000)]          public string? PageSubtitle    { get; set; }
    [MaxLength(128)]           public string? NewProjectLabel { get; set; }

    [MaxLength(128)]  public string? BoardTabLabel     { get; set; }
    [MaxLength(128)]  public string? WeekTabLabel      { get; set; }
    [MaxLength(128)]  public string? AttentionTabLabel { get; set; }
    [MaxLength(128)]  public string? StagesTabLabel    { get; set; }
    [MaxLength(2000)] public string? StagesIntro       { get; set; }

    [MaxLength(256)]  public string? AttentionTitle { get; set; }
    [MaxLength(256)]  public string? ScheduleTitle  { get; set; }
    [MaxLength(256)]  public string? TimeTitle      { get; set; }
    [MaxLength(2000)] public string? TimeFootnote   { get; set; }
    [MaxLength(256)]  public string? SeasonTitle    { get; set; }
    [MaxLength(2000)] public string? SeasonSubtitle { get; set; }
}

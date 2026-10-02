using Volo.Abp.Domain.Entities.Auditing;

namespace DymoEnergy.ProjectPlanning;

/// <summary>Single-row table with every label / colour the admin can customise on the page.</summary>
public class ProjectPlanningSetting : AuditedAggregateRoot<int>
{
    public string AccentColor      { get; set; } = "#0E6B3F";
    public string CurrencySymbol   { get; set; } = "৳";
    public int    DueSoonDays      { get; set; } = 3;

    public string PageTitle        { get; set; } = "Project planning";
    public string PageSubtitle     { get; set; } = "Every installation job from accepted quote to handover.";
    public string NewProjectLabel  { get; set; } = "New project";

    public string BoardTabLabel     { get; set; } = "Board";
    public string WeekTabLabel      { get; set; } = "This week";
    public string AttentionTabLabel { get; set; } = "Needs attention";
    public string StagesTabLabel    { get; set; } = "Stages & checklist";
    public string StagesIntro       { get; set; } = "The steps every installation goes through. A job cannot move to the next stage until its ticks are done — this is what keeps jobs from getting stuck.";

    public string AttentionTitle   { get; set; } = "Jobs that need a decision today";
    public string ScheduleTitle    { get; set; } = "Crew schedule";
    public string TimeTitle        { get; set; } = "Where time goes";
    public string TimeFootnote     { get; set; } = string.Empty;
    public string SeasonTitle      { get; set; } = "Season plan";
    public string SeasonSubtitle   { get; set; } = "Book installations around the weather and the farming season.";
}

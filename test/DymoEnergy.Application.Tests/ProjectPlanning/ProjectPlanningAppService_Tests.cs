using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace DymoEnergy.ProjectPlanning;

public abstract class ProjectPlanningAppService_Tests<TStartupModule> : DymoEnergyApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IProjectPlanningAppService _service;

    protected ProjectPlanningAppService_Tests()
    {
        _service = GetRequiredService<IProjectPlanningAppService>();
    }

    [Fact]
    public async Task Should_Seed_Editable_Defaults_On_First_Open()
    {
        var board = await _service.GetBoardAsync(new ProjectPlanningFilterDto());

        board.Stages.Count.ShouldBe(7);
        board.Stages.Select(s => s.Stage.Name).First().ShouldBe("Site survey");
        board.Stages.Last().Stage.IsFinal.ShouldBeTrue();
        board.Stages.All(s => s.Stage.Checklist.Count == 4).ShouldBeTrue();
        board.Teams.Count.ShouldBe(3);
        board.Setting.PageTitle.ShouldBe("Project planning");

        // Opening again must not seed twice.
        (await _service.GetStagesAsync()).Count.ShouldBe(7);
    }

    [Fact]
    public async Task Should_Customise_Setting_Stage_Team_And_Season_Notes()
    {
        var setting = await _service.GetSettingAsync();
        setting.PageTitle = "Installation jobs";
        setting.AccentColor = "#2563EB";
        setting.WeekTabLabel = "Crew week";
        await _service.UpdateSettingAsync(setting);

        var reloaded = await _service.GetSettingAsync();
        reloaded.PageTitle.ShouldBe("Installation jobs");
        reloaded.AccentColor.ShouldBe("#2563EB");
        reloaded.WeekTabLabel.ShouldBe("Crew week");

        var stages = await _service.GetStagesAsync();
        var survey = stages.First();
        await _service.UpdateStageAsync(survey.Id, new CreateUpdateProjectStageDto
        {
            Name = "Roof visit", Color = "#7C3AED", Order = survey.Order, IsActive = true, RequiresScheduling = true,
            Checklist = new() { "Measure", "  ", "Photograph" },
        });
        var renamed = (await _service.GetStagesAsync()).First(s => s.Id == survey.Id);
        renamed.Name.ShouldBe("Roof visit");
        renamed.Color.ShouldBe("#7C3AED");
        renamed.Checklist.ShouldBe(new[] { "Measure", "Photograph" });

        var extra = await _service.CreateStageAsync(new CreateUpdateProjectStageDto { Name = "Warranty check", Color = "#0F766E", IsActive = true });
        extra.Order.ShouldBe(stages.Max(s => s.Order) + 1);

        var team = await _service.CreateTeamAsync(new CreateUpdateProjectTeamDto { Name = "Team C", Color = "#DB2777", IsActive = true });
        (await _service.GetTeamsAsync()).Select(t => t.Name).ShouldContain("Team C");
        await _service.DeleteTeamAsync(team.Id);
        (await _service.GetTeamsAsync()).Select(t => t.Name).ShouldNotContain("Team C");

        var note = await _service.CreateSeasonNoteAsync(new CreateUpdateProjectSeasonNoteDto { Title = "Eid", Period = "Mar", Color = "#6B7280" });
        (await _service.GetSeasonNotesAsync()).ShouldContain(n => n.Id == note.Id);
    }

    [Fact]
    public async Task Should_Block_Moving_Forward_Until_Checklist_Is_Ticked()
    {
        var stages  = await _service.GetStagesAsync();
        var survey  = stages[0];
        var design  = stages[1];

        var project = await _service.CreateProjectAsync(new CreateUpdateProjectDto
        {
            CustomerName = "Rafiq Islam", Title = "3 kW on-grid", District = "Chattogram", Value = 380000,
            Tags = new() { "Large job", "Farm" },
        });
        project.Code.ShouldStartWith("PRJ-");
        project.StageId.ShouldBe(survey.Id);
        project.Tags.ShouldBe(new[] { "Large job", "Farm" });

        // Unticked checklist → refused with a recognisable code.
        var ex = await Should.ThrowAsync<BusinessException>(() =>
            _service.UpdateProjectStageAsync(project.Id, new MoveProjectStageDto { StageId = design.Id }));
        ex.Code.ShouldBe(ProjectPlanningAppService.ChecklistIncompleteCode);

        // Tick every item, then the move is accepted.
        await _service.UpdateProjectAsync(project.Id, new CreateUpdateProjectDto
        {
            CustomerName = project.CustomerName,
            ChecklistDone = survey.Checklist.Select(c => $"{survey.Id}|{c}").ToList(),
        });
        var moved = await _service.UpdateProjectStageAsync(project.Id, new MoveProjectStageDto { StageId = design.Id });
        moved.StageId.ShouldBe(design.Id);
        moved.StageName.ShouldBe(design.Name);

        // Force skips the rule.
        var forced = await _service.UpdateProjectStageAsync(project.Id, new MoveProjectStageDto { StageId = stages[2].Id, Force = true });
        forced.StageId.ShouldBe(stages[2].Id);

        // Moving backwards never needs ticks.
        (await _service.UpdateProjectStageAsync(project.Id, new MoveProjectStageDto { StageId = survey.Id })).StageId.ShouldBe(survey.Id);

        // A stage that still holds projects cannot be deleted.
        await Should.ThrowAsync<UserFriendlyException>(() => _service.DeleteStageAsync(survey.Id));
    }

    [Fact]
    public async Task Should_Report_Blocked_And_Late_Jobs_Under_Needs_Attention()
    {
        var yesterday = DateTime.Today.AddDays(-1);

        var blocked = await _service.CreateProjectAsync(new CreateUpdateProjectDto
        {
            CustomerName = "Customer M", Value = 2210000, IsBlocked = true, BlockedReason = "8 panels short",
            WaitingFor = "Supplier A", WaitingSince = DateTime.Today.AddDays(-5), ActionLabel = "Call supplier",
        });
        await _service.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Customer U", DueDate = yesterday });
        await _service.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Customer Q", DueDate = DateTime.Today.AddDays(1) });
        await _service.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Customer Z" });

        var attention = await _service.GetAttentionAsync(new ProjectPlanningFilterDto());
        attention.Running.ShouldBe(4);
        attention.Blocked.ShouldBe(1);
        attention.Late.ShouldBe(1);
        attention.Items.Count.ShouldBe(2);
        attention.Items[0].CustomerName.ShouldBe("Customer M"); // oldest wait first
        attention.Items[0].WaitingDays.ShouldBe(5);
        attention.Items[0].ActionLabel.ShouldBe("Call supplier");
        attention.TimePerStage.Count.ShouldBe(7);
        attention.SeasonNotes.Count.ShouldBe(4);

        var board = await _service.GetBoardAsync(new ProjectPlanningFilterDto());
        board.AttentionCount.ShouldBe(2);
        board.Projects.First(p => p.CustomerName == "Customer Q").IsDueSoon.ShouldBeTrue();

        // Unblocking clears the problem fields.
        var fixedUp = await _service.UpdateProjectAsync(blocked.Id, new CreateUpdateProjectDto { CustomerName = "Customer M", IsBlocked = false, BlockedReason = "stale" });
        fixedUp.BlockedReason.ShouldBeNull();
        (await _service.GetAttentionAsync(new ProjectPlanningFilterDto())).Blocked.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Build_The_Week_Schedule_And_Waiting_List()
    {
        var teams = await _service.GetTeamsAsync();
        var teamA = teams[0];

        var booked  = await _service.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Booked job", Title = "5 kW hybrid", MaterialStatus = ProjectMaterialStatus.Ready, MaterialNote = "Packed" });
        var waiting = await _service.CreateProjectAsync(new CreateUpdateProjectDto { CustomerName = "Waiting job" });

        // Friday is the day off and not a grid column, so book on Saturday when the tests run on a Friday.
        var today = DateTime.Today.DayOfWeek == DayOfWeek.Friday ? DateTime.Today.AddDays(1) : DateTime.Today;
        await _service.CreateScheduleEntryAsync(new CreateUpdateProjectScheduleEntryDto { ProjectId = booked.Id, TeamId = teamA.Id, Date = today, Note = "Halishahar" });

        var week = await _service.GetWeekAsync(new ProjectWeekFilterDto { Date = today });
        week.WeekStart.DayOfWeek.ShouldBe(DayOfWeek.Saturday);
        (week.WeekStart <= today && today < week.WeekStart.AddDays(6)).ShouldBeTrue();

        var row = week.Rows.First(r => r.Team.Id == teamA.Id);
        var entry = row.Entries.ShouldHaveSingleItem();
        entry.Title.ShouldBe("Booked job · 5 kW hybrid");
        entry.Note.ShouldBe("Halishahar");

        // The first stage needs a crew: only the job without a booking is waiting.
        week.Waiting.Select(p => p.CustomerName).ShouldBe(new[] { "Waiting job" });
        week.Materials.Select(p => p.CustomerName).ShouldBe(new[] { "Booked job" });

        // Moving a booking to another team keeps one entry.
        await _service.UpdateScheduleEntryAsync(entry.Id, new CreateUpdateProjectScheduleEntryDto { ProjectId = booked.Id, TeamId = teams[1].Id, Date = today, Note = entry.Note });
        var moved = await _service.GetWeekAsync(new ProjectWeekFilterDto { Date = today });
        moved.Rows.First(r => r.Team.Id == teamA.Id).Entries.ShouldBeEmpty();
        moved.Rows.First(r => r.Team.Id == teams[1].Id).Entries.ShouldHaveSingleItem();

        // A team that is used cannot be deleted.
        await Should.ThrowAsync<UserFriendlyException>(() => _service.DeleteTeamAsync(teams[1].Id));

        await _service.DeleteScheduleEntryAsync(entry.Id);
        (await _service.GetWeekAsync(new ProjectWeekFilterDto { Date = today })).Rows.SelectMany(r => r.Entries).ShouldBeEmpty();

        // Team filter narrows the grid to that team.
        (await _service.GetWeekAsync(new ProjectWeekFilterDto { Date = today, TeamId = teamA.Id })).Rows.ShouldHaveSingleItem();
    }
}

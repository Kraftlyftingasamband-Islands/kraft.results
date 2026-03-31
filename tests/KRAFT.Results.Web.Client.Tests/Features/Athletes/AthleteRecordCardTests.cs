using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthleteRecordCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsSingleLiftBadge_WhenIsSingleLift()
    {
        // Arrange
        AthleteRecord record = new(
            Date: new DateOnly(2024, 3, 15),
            IsClassic: true,
            IsSingleLift: true,
            WeightCategory: "83",
            AgeCategory: "Open",
            Type: Constants.Bench,
            Weight: 150.0m,
            Meet: "Test Meet",
            MeetSlug: "test-meet");

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.FindAll(".ar-sl-badge").Count.ShouldBe(1);
        cut.Find(".ar-sl-badge").TextContent.ShouldBe(Constants.SingeLift);
    }

    [Fact]
    public void HidesSingleLiftBadge_WhenIsNotSingleLift()
    {
        // Arrange
        AthleteRecord record = new(
            Date: new DateOnly(2024, 3, 15),
            IsClassic: false,
            IsSingleLift: false,
            WeightCategory: "83",
            AgeCategory: "Open",
            Type: Constants.Total,
            Weight: 600.0m,
            Meet: "Test Meet",
            MeetSlug: "test-meet");

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.FindAll(".ar-sl-badge").Count.ShouldBe(0);
    }

    [Fact]
    public void ShowsWeightCategory_InBadge()
    {
        // Arrange
        AthleteRecord record = new(
            Date: new DateOnly(2024, 3, 15),
            IsClassic: false,
            IsSingleLift: false,
            WeightCategory: "93",
            AgeCategory: "Open",
            Type: Constants.Total,
            Weight: 700.0m,
            Meet: "Test Meet",
            MeetSlug: "test-meet");

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.Find(".ar-cat").TextContent.ShouldBe("93");
    }

    [Fact]
    public void WeightLinkPointsToMeet()
    {
        // Arrange
        AthleteRecord record = new(
            Date: new DateOnly(2024, 3, 15),
            IsClassic: false,
            IsSingleLift: false,
            WeightCategory: "83",
            AgeCategory: "Open",
            Type: Constants.Total,
            Weight: 600.0m,
            Meet: "Spring Open",
            MeetSlug: "spring-open-2024");

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.Find("a.ar-mark-link").GetAttribute("href").ShouldBe("/meets/spring-open-2024");
    }

    public void Dispose() => _context.Dispose();
}
using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthletePersonalBestCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsSingleLiftBadge_WhenIsSingleLift()
    {
        // Arrange
        AthletePersonalBest best = new(
            IsClassic: false,
            IsSingleLift: true,
            Discipline: Discipline.Bench,
            Weight: 147.5m,
            WeightCategory: "83 kg",
            BodyWeight: 82.50m,
            MeetSlug: "test-meet",
            MeetType: Constants.Powerlifting,
            Date: new DateOnly(2024, 3, 15));

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.FindAll(".apb-sl-badge").Count.ShouldBe(1);
        cut.Find(".apb-sl-badge").TextContent.ShouldBe(Constants.SingeLift);
    }

    [Fact]
    public void HidesSingleLiftBadge_WhenIsNotSingleLift()
    {
        // Arrange
        AthletePersonalBest best = new(
            IsClassic: false,
            IsSingleLift: false,
            Discipline: Discipline.Squat,
            Weight: 245.0m,
            WeightCategory: "83 kg",
            BodyWeight: 82.50m,
            MeetSlug: "test-meet",
            MeetType: Constants.Powerlifting,
            Date: new DateOnly(2024, 3, 15));

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.FindAll(".apb-sl-badge").Count.ShouldBe(0);
    }

    [Fact]
    public void ShowsWeight_WithOneDecimalPlace()
    {
        // Arrange
        AthletePersonalBest best = new(
            IsClassic: false,
            IsSingleLift: false,
            Discipline: Discipline.None,
            Weight: 697.5m,
            WeightCategory: "83 kg",
            BodyWeight: 82.50m,
            MeetSlug: "test-meet",
            MeetType: Constants.Powerlifting,
            Date: new DateOnly(2024, 3, 15));

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find(".apb-weight").TextContent.ShouldContain("697.5");
    }

    [Fact]
    public void CardLinksToMeet()
    {
        // Arrange
        AthletePersonalBest best = new(
            IsClassic: false,
            IsSingleLift: false,
            Discipline: Discipline.Squat,
            Weight: 245.0m,
            WeightCategory: "83 kg",
            BodyWeight: 82.50m,
            MeetSlug: "spring-open-2024",
            MeetType: Constants.Powerlifting,
            Date: new DateOnly(2024, 3, 15));

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find("a.apb-card-link").GetAttribute("href").ShouldBe("/meets/spring-open-2024");
    }

    public void Dispose() => _context.Dispose();
}
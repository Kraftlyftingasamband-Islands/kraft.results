using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthleteParticipationCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsMedalEmoji_ForTopThreeRanks()
    {
        // Arrange & Act
        IRenderedComponent<AthleteParticipationCard> cut1 = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation(rank: 1))
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        IRenderedComponent<AthleteParticipationCard> cut2 = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation(rank: 2))
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut1.Find(".ap-rank").TextContent.Trim().ShouldBe("\U0001f947");
        cut2.Find(".ap-rank").TextContent.Trim().ShouldBe("\U0001f948");
    }

    [Fact]
    public void ShowsDash_WhenDisqualified()
    {
        // Arrange & Act
        IRenderedComponent<AthleteParticipationCard> cut = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation(rank: 1, isDisqualified: true))
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut.Find(".ap-rank").TextContent.Trim().ShouldBe("\u2013");
    }

    [Fact]
    public void HidesLiftsRow_WhenDisqualified()
    {
        // Arrange & Act
        IRenderedComponent<AthleteParticipationCard> cut = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation(isDisqualified: true))
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut.FindAll(".ap-lifts").Count.ShouldBe(0);
    }

    [Fact]
    public void ShowsAllFourPills_ForPowerlifting()
    {
        // Arrange & Act
        IRenderedComponent<AthleteParticipationCard> cut = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation())
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut.FindAll(".ap-lift-pill").Count.ShouldBe(4);
    }

    [Fact]
    public void ShowsOneBenchPill_ForBenchMeet()
    {
        // Arrange
        AthleteParticipation benchParticipation = new(
            Date: new DateOnly(2024, 3, 15),
            Meet: "Bekkpressumot",
            MeetSlug: "bench-meet",
            MeetType: Constants.Bench,
            Club: null,
            ClubSlug: null,
            Rank: 1,
            WeightCategory: "83",
            BodyWeight: 82.50m,
            Squat: 0,
            Benchpress: 147.5m,
            Deadlift: 0,
            Total: 0,
            Wilks: 0,
            IpfPoints: 100.0m,
            IsDisqualified: false);

        // Act
        IRenderedComponent<AthleteParticipationCard> cut = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, benchParticipation)
                  .Add(c => c.MeetType, Constants.Bench)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut.FindAll(".ap-lift-pill").Count.ShouldBe(1);
    }

    [Fact]
    public void ShowsClub_WhenShowClubIsTrue()
    {
        // Arrange & Act
        IRenderedComponent<AthleteParticipationCard> cut = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation(club: "IR", clubSlug: "ir"))
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, true)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut.FindAll(".ap-club").Count.ShouldBe(1);
        cut.Find(".ap-club").TextContent.ShouldContain("IR");
    }

    [Fact]
    public void HidesClub_WhenShowClubIsFalse()
    {
        // Arrange & Act
        IRenderedComponent<AthleteParticipationCard> cut = _context.Render<AthleteParticipationCard>(
            p => p.Add(c => c.Participation, MakeParticipation(club: "IR", clubSlug: "ir"))
                  .Add(c => c.MeetType, Constants.Powerlifting)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.ShowIpfPoints, true));

        // Assert
        cut.FindAll(".ap-club").Count.ShouldBe(0);
    }

    public void Dispose() => _context.Dispose();

    private static AthleteParticipation MakeParticipation(
        int rank = 1,
        bool isDisqualified = false,
        decimal squat = 245.0m,
        decimal bench = 147.5m,
        decimal deadlift = 310.0m,
        decimal total = 702.5m,
        decimal? ipfPoints = 412.34m,
        string? club = null,
        string? clubSlug = null) =>
        new(
            Date: new DateOnly(2024, 3, 15),
            Meet: "Islandsmot 2024",
            MeetSlug: "islandsm-t-2024",
            MeetType: Constants.Powerlifting,
            Club: club,
            ClubSlug: clubSlug,
            Rank: rank,
            WeightCategory: "83",
            BodyWeight: 82.50m,
            Squat: squat,
            Benchpress: bench,
            Deadlift: deadlift,
            Total: total,
            Wilks: 0,
            IpfPoints: ipfPoints,
            IsDisqualified: isDisqualified);
}
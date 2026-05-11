using Bunit;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Components;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class MeetCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    public MeetCardTests()
    {
        _context.AddAuthorization();
    }

    [Fact]
    public void RendersArticleWithMeetCardClass_WhenPastMeet()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("article.meet-card").ShouldNotBeNull();
    }

    [Fact]
    public void DoesNotHaveGhostModifier_WhenPastMeet()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.FindAll("article.meet-card--ghost").Count.ShouldBe(0);
    }

    [Fact]
    public void HasGhostModifier_WhenFutureMeet()
    {
        // Arrange
        MeetSummary meet = MakeFutureMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet)
                  .Add(c => c.IsFuture, true));

        // Assert
        cut.Find("article.meet-card--ghost").ShouldNotBeNull();
    }

    [Fact]
    public void RendersDisciplinePill_WithDisciplineText()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".pill-discipline").TextContent.Trim().ShouldBe("Kraftlyftingar");
    }

    [Fact]
    public void RendersClassicEquipmentPill_WhenIsClassicTrue()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".pill-classic").TextContent.Trim().ShouldBe("Án búnaðar");
    }

    [Fact]
    public void RendersEquippedEquipmentPill_WhenIsClassicFalse()
    {
        // Arrange
        MeetSummary meet = new(
            Slug: "equipped-meet",
            Title: "Equipped Meet",
            Location: "Reykjavík",
            StartDate: new DateOnly(2020, 3, 15),
            Discipline: "Kraftlyftingar",
            IsClassic: false,
            ParticipantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".pill-equipped").TextContent.Trim().ShouldBe("Með búnaði");
    }

    [Fact]
    public void RendersParticipantCountAsKeppendur_WhenPastMeetWithParticipants()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 42);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".meet-card-count").TextContent.Trim().ShouldBe("42 keppendur");
    }

    [Fact]
    public void RendersParticipantCountAsSkladir_WhenFutureMeetWithRegistrations()
    {
        // Arrange
        MeetSummary meet = MakeFutureMeet(participantCount: 15);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet)
                  .Add(c => c.IsFuture, true));

        // Assert
        cut.Find(".meet-card-count").TextContent.Trim().ShouldBe("15 skráðir");
    }

    [Fact]
    public void HidesParticipantCount_WhenZeroParticipantsAndNonAdmin()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.FindAll(".meet-card-count").Count.ShouldBe(0);
    }

    [Fact]
    public void ShowsParticipantCount_WhenZeroParticipantsAndAdmin()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);
        _context.AddAuthorization().SetRoles("Admin");

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".meet-card-count").ShouldNotBeNull();
    }

    [Fact]
    public void RendersTitleAsNavLink_WhenParticipantCountGreaterThanZero()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 10);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("a.meet-card-title-link").ShouldNotBeNull();
        cut.FindAll("span.meet-card-title-text").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersTitleAsNavLink_WhenZeroParticipantsAndAdmin()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);
        _context.AddAuthorization().SetRoles("Admin");

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("a.meet-card-title-link").ShouldNotBeNull();
        cut.FindAll("span.meet-card-title-text").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersTitleAsPlainText_WhenZeroParticipantsAndNonAdmin()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("span.meet-card-title-text").ShouldNotBeNull();
        cut.FindAll("a.meet-card-title-link").Count.ShouldBe(0);
    }

    [Fact]
    public void DoesNotHaveLinkedModifier_WhenZeroParticipantsAndNonAdmin()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("article.meet-card--linked").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void HasLinkedModifier_WhenParticipantCountGreaterThanZero()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 5);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("article.meet-card--linked").ShouldNotBeNull();
        });
    }

    [Fact]
    public void HasLinkedModifier_WhenZeroParticipantsAndAdmin()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);
        _context.AddAuthorization().SetRoles("Admin");

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("article.meet-card--linked").ShouldNotBeNull();
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static MeetSummary MakePastMeet(int participantCount = 0) =>
        new(
            Slug: "nationals-2024",
            Title: "Nationals 2024",
            Location: "Reykjavík",
            StartDate: new DateOnly(2020, 3, 15),
            Discipline: "Kraftlyftingar",
            IsClassic: true,
            ParticipantCount: participantCount);

    private static MeetSummary MakeFutureMeet(int participantCount = 0) =>
        new(
            Slug: "nationals-2030",
            Title: "Nationals 2030",
            Location: "Reykjavík",
            StartDate: new DateOnly(2030, 3, 15),
            Discipline: "Kraftlyftingar",
            IsClassic: true,
            ParticipantCount: participantCount);
}
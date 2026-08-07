using Bunit;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets.Components;

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
    public void WhenRendered_ArticleMeetCardClassAlwaysPresent()
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
    public void WhenPastMeetWithParticipants_ArticleHasNoModifier()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 10);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.FindAll("article.meet-card--ghost").Count.ShouldBe(0);
        cut.FindAll("article.meet-card--active").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenPastMeetWithNoParticipants_HasGhostModifier()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("article.meet-card--ghost").ShouldNotBeNull();
    }

    [Fact]
    public void WhenUpcomingMeetWithNoParticipants_HasGhostModifier()
    {
        // Arrange
        MeetSummary meet = MakeUpcomingMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("article.meet-card--ghost").ShouldNotBeNull();
    }

    [Fact]
    public void WhenUpcomingMeetWithParticipants_HasActiveModifier()
    {
        // Arrange
        MeetSummary meet = MakeUpcomingMeet(participantCount: 15);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("article.meet-card--active").ShouldNotBeNull();
    }

    [Fact]
    public void CategoryPillRendersFirst_BeforeEquipmentPill()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        IReadOnlyList<AngleSharp.Dom.IElement> pills = cut.FindAll(".esc-pill");
        pills.Count.ShouldBe(2);
        pills[0].ClassList.ShouldContain("esc-pill--meet-category");
        pills[1].ClassList.ShouldContain("esc-pill--equipment-classic");
    }

    [Fact]
    public void CategoryPillDisplaysCategoryText()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-pill--meet-category").TextContent.Trim().ShouldBe("Kraftlyftingar");
    }

    [Fact]
    public void WhenIsClassicTrue_RendersClassicEquipmentPill()
    {
        // Arrange
        MeetSummary meet = MakePastMeet();

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-pill--equipment-classic").TextContent.Trim().ShouldBe("Án búnaðar");
    }

    [Fact]
    public void WhenIsClassicFalse_RendersEquippedEquipmentPill()
    {
        // Arrange
        MeetSummary meet = new(
            Slug: "equipped-meet",
            Title: "Equipped Meet",
            Location: "Reykjavík",
            StartDate: new DateOnly(2020, 3, 15),
            Category: "Kraftlyftingar",
            IsClassic: false,
            IsUpcoming: false,
            ParticipantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-pill--equipment-equipped").TextContent.Trim().ShouldBe("Með búnaði");
    }

    [Fact]
    public void WhenLocationIsNull_MetaShowsDateOnly()
    {
        // Arrange
        MeetSummary meet = new(
            Slug: "no-location",
            Title: "No Location Meet",
            Location: null,
            StartDate: new DateOnly(2020, 3, 15),
            Category: "Kraftlyftingar",
            IsClassic: true,
            IsUpcoming: false,
            ParticipantCount: 10);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-meta").TextContent.Trim().ShouldBe("15. mar. 2020");
    }

    [Fact]
    public void WhenLocationIsSet_MetaShowsDateAndLocation()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 10);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-meta").TextContent.Trim().ShouldContain("15. mar. 2020");
        cut.Find(".esc-meta").TextContent.Trim().ShouldContain("Reykjavík");
    }

    [Fact]
    public void WhenPastMeetWithParticipants_LabelIsKeppendur()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 42);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-label").TextContent.Trim().ShouldBe("keppendur");
    }

    [Fact]
    public void WhenUpcomingMeetWithParticipants_LabelIsSkladir()
    {
        // Arrange
        MeetSummary meet = MakeUpcomingMeet(participantCount: 15);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".esc-label").TextContent.Trim().ShouldBe("skráðir");
    }

    [Fact]
    public void WhenActiveMeet_RendersSkraningOpinBadge()
    {
        // Arrange
        MeetSummary meet = MakeUpcomingMeet(participantCount: 15);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find(".card-status--active").TextContent.Trim().ShouldBe("Skráning opin");
    }

    [Fact]
    public void WhenNotActiveMeet_DoesNotRenderSkraningOpinBadge()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 10);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.FindAll(".card-status--active").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenGhostNonAdmin_TitleIsPlainSpan()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("span.esc-name").ShouldNotBeNull();
            cut.FindAll("a.esc-name").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenGhostNonAdmin_NoStatColumn()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".esc-stat").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenGhostNonAdmin_NotClickable()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 0);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("article.card--clickable").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenGhostAdmin_TitleIsLink()
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
            cut.Find("a.esc-name").ShouldNotBeNull();
            cut.FindAll("span.esc-name").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenGhostAdmin_StatShowsZero()
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
            cut.Find(".esc-value").TextContent.Trim().ShouldBe("0");
        });
    }

    [Fact]
    public void WhenGhostAdmin_IsClickable()
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
            cut.Find("article.card--clickable").ShouldNotBeNull();
        });
    }

    [Fact]
    public void WhenMeetHasParticipants_TitleIsLink()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 10);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.Find("a.esc-name").ShouldNotBeNull();
        cut.FindAll("span.esc-name").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenMeetHasParticipants_IsClickable()
    {
        // Arrange
        MeetSummary meet = MakePastMeet(participantCount: 5);

        // Act
        IRenderedComponent<MeetCard> cut = _context.Render<MeetCard>(
            p => p.Add(c => c.Meet, meet));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("article.card--clickable").ShouldNotBeNull();
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
            Category: "Kraftlyftingar",
            IsClassic: true,
            IsUpcoming: false,
            ParticipantCount: participantCount);

    private static MeetSummary MakeUpcomingMeet(int participantCount = 0) =>
        new(
            Slug: "nationals-2030",
            Title: "Nationals 2030",
            Location: "Reykjavík",
            StartDate: new DateOnly(2030, 3, 15),
            Category: "Kraftlyftingar",
            IsClassic: true,
            IsUpcoming: true,
            ParticipantCount: participantCount);
}

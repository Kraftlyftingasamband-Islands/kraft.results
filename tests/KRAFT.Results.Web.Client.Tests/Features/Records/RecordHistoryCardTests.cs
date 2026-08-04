using Bunit;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Web.Client.Features.Records;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Records;

public sealed class RecordHistoryCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenCurrentNonStandard_RendersNuverandiMetPill()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".card-status--active").TextContent.ShouldBe("Núverandi met");
    }

    [Fact]
    public void WhenCurrentNonStandard_AppliesRhcCurrentCssClass()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".rhc-current").ShouldNotBeNull();
    }

    [Fact]
    public void WhenCurrentNonStandard_RendersBodyweightPill()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        AngleSharp.Dom.IElement pill = cut.Find(".esc-pill");
        pill.TextContent.ShouldBe("82,50 kg");
    }

    [Fact]
    public void WhenCurrentNonStandard_RendersMeetLink()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        AngleSharp.Dom.IElement link = cut.Find("a[href='/meets/nationals-2024']");
        link.TextContent.ShouldBe("Nationals 2024");
    }

    [Fact]
    public void WhenCurrentNonStandard_RendersGreenDelta()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".rhc-delta").TextContent.ShouldBe("▲ +5,0");
    }

    [Fact]
    public void WhenCurrentNonStandard_RendersWeightWithKgUnit()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".esc-value").TextContent.Trim().ShouldBe("200,5kg");
    }

    [Fact]
    public void WhenCurrentNonStandard_RendersAthleteNameAsLink()
    {
        // Arrange
        RecordHistoryEntry entry = MakeCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        AngleSharp.Dom.IElement link = cut.Find("a.esc-name");
        link.GetAttribute("href").ShouldBe("/athletes/jon-jonsson");
        link.TextContent.ShouldBe("Jón Jónsson");
    }

    [Fact]
    public void WhenNotCurrent_DoesNotRenderStatusPill()
    {
        // Arrange
        RecordHistoryEntry entry = MakeNonCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.FindAll(".card-status--active").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenNotCurrent_DoesNotApplyRhcCurrentCssClass()
    {
        // Arrange
        RecordHistoryEntry entry = MakeNonCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.FindAll(".rhc-current").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenStandard_RendersIslenskiStadallinnAsPlainText()
    {
        // Arrange
        RecordHistoryEntry entry = MakeStandardEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
        cut.Find("span.esc-name").TextContent.ShouldBe("Íslenski staðallinn");
    }

    [Fact]
    public void WhenStandard_DoesNotRenderMeetLink()
    {
        // Arrange
        RecordHistoryEntry entry = MakeStandardEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.FindAll("a[href='/meets/nationals-2024']").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenStandard_WithDelta_RendersDeltaSpan()
    {
        // Arrange
        RecordHistoryEntry entry = MakeStandardEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".rhc-delta").TextContent.ShouldBe("▲ +5,0");
    }

    [Fact]
    public void WhenStandard_WithNullDelta_DoesNotRenderDeltaSpan()
    {
        // Arrange
        RecordHistoryEntry entry = new(
            Date: new DateOnly(2024, 1, 15),
            Athlete: null,
            AthleteSlug: null,
            Weight: 200m,
            BodyWeight: null,
            Meet: "Nationals 2024",
            MeetSlug: "nationals-2024",
            IsCurrent: false,
            IsStandard: true,
            Delta: null);

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.FindAll(".rhc-delta").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenStandardAndCurrent_AppliesRhcCurrentCssClass()
    {
        // Arrange
        RecordHistoryEntry entry = MakeStandardCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".rhc-current").ShouldNotBeNull();
    }

    [Fact]
    public void WhenStandardAndCurrent_RendersNuverandiMetPill()
    {
        // Arrange
        RecordHistoryEntry entry = MakeStandardCurrentEntry();

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.Find(".card-status--active").TextContent.ShouldBe("Núverandi met");
    }

    [Fact]
    public void WhenNoDelta_DoesNotRenderDeltaSpan()
    {
        // Arrange
        RecordHistoryEntry entry = new(
            Date: new DateOnly(2024, 1, 15),
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            Weight: 200m,
            BodyWeight: null,
            Meet: "Nationals 2024",
            MeetSlug: "nationals-2024",
            IsCurrent: false,
            IsStandard: false,
            Delta: null);

        // Act
        IRenderedComponent<RecordHistoryCard> cut = _context.Render<RecordHistoryCard>(
            parameters => parameters.Add(p => p.Entry, entry));

        // Assert
        cut.FindAll(".rhc-delta").Count.ShouldBe(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static RecordHistoryEntry MakeCurrentEntry() =>
        new(
            Date: new DateOnly(2024, 1, 15),
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            Weight: 200.5m,
            BodyWeight: 82.5m,
            Meet: "Nationals 2024",
            MeetSlug: "nationals-2024",
            IsCurrent: true,
            IsStandard: false,
            Delta: 5.0m);

    private static RecordHistoryEntry MakeNonCurrentEntry() =>
        new(
            Date: new DateOnly(2024, 1, 15),
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            Weight: 195.5m,
            BodyWeight: 82.5m,
            Meet: "Nationals 2023",
            MeetSlug: "nationals-2023",
            IsCurrent: false,
            IsStandard: false,
            Delta: null);

    private static RecordHistoryEntry MakeStandardEntry() =>
        new(
            Date: new DateOnly(2024, 1, 15),
            Athlete: null,
            AthleteSlug: null,
            Weight: 200m,
            BodyWeight: null,
            Meet: "Nationals 2024",
            MeetSlug: "nationals-2024",
            IsCurrent: false,
            IsStandard: true,
            Delta: 5.0m);

    private static RecordHistoryEntry MakeStandardCurrentEntry() =>
        new(
            Date: new DateOnly(2024, 1, 15),
            Athlete: null,
            AthleteSlug: null,
            Weight: 210m,
            BodyWeight: null,
            Meet: "Nationals 2024",
            MeetSlug: "nationals-2024",
            IsCurrent: true,
            IsStandard: true,
            Delta: 10.0m);
}

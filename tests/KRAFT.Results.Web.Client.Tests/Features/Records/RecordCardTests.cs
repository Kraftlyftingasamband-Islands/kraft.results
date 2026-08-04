using Bunit;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Web.Client.Features.Records;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Records;

public sealed class RecordCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenNonStandard_RendersBadgeWithWeightCategory()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.Find(".esc-leading--badge").TextContent.ShouldBe("83");
    }

    [Fact]
    public void WhenNonStandard_RendersBadgeAsAnchorLinkingToRecordHistory()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 42,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        AngleSharp.Dom.IElement badgeLink = cut.Find("a.esc-leading--badge");
        badgeLink.GetAttribute("href").ShouldBe("/records/42/history");
        badgeLink.TextContent.ShouldBe("83");
    }

    [Fact]
    public void WhenNonStandard_BadgeAnchorHasIcelandicAriaLabel()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 7,
            WeightCategory: "-59",
            Athlete: "Anna Sigurðardóttir",
            AthleteSlug: "anna-sigurdardottir",
            BirthYear: null,
            BodyWeight: null,
            Weight: 95.0m,
            Date: new DateOnly(2024, 5, 10),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        AngleSharp.Dom.IElement badgeLink = cut.Find("a.esc-leading--badge");
        badgeLink.GetAttribute("aria-label").ShouldBe("Skoða met-sögu fyrir -59 kg");
    }

    [Fact]
    public void WhenStandard_BadgeSpanHasNoAriaLabel()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 7,
            WeightCategory: "-59",
            Athlete: "Anna Sigurðardóttir",
            AthleteSlug: "anna-sigurdardottir",
            BirthYear: null,
            BodyWeight: null,
            Weight: 95.0m,
            Date: new DateOnly(2024, 5, 10),
            MeetSlug: "nationals-2024",
            IsStandard: true);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        AngleSharp.Dom.IElement badgeSpan = cut.Find("span.esc-leading--badge");
        badgeSpan.GetAttribute("aria-label").ShouldBeNull();
    }

    [Fact]
    public void WhenNonStandard_ValueLinkHasMeetAriaLabel()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: null,
            BodyWeight: null,
            Weight: 120.0m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.Find("a.esc-value").GetAttribute("aria-label").ShouldBe("120,0 kg, opna mót");
    }

    [Fact]
    public void WhenNonStandard_RendersAthleteNameAsLink()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        AngleSharp.Dom.IElement link = cut.Find("a.esc-name");
        link.GetAttribute("href").ShouldBe("/athletes/jon-jonsson");
        link.TextContent.ShouldBe("Jón Jónsson");
    }

    [Fact]
    public void WhenNonStandard_RendersBirthYearAndBodyweightPills()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        System.Collections.Generic.IReadOnlyList<AngleSharp.Dom.IElement> pills = cut.FindAll(".esc-pill");
        pills.Count.ShouldBe(2);
        pills[0].TextContent.ShouldBe("1990");
        pills[1].TextContent.ShouldBe("82,50 kg");
    }

    [Fact]
    public void WhenNonStandard_RendersDateAsMetaText()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: null,
            BodyWeight: null,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.Find(".esc-meta").TextContent.ShouldBe("15.03.2024");
    }

    [Fact]
    public void WhenNonStandard_RendersWeightAsValueLinkingToMeet()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: null,
            BodyWeight: null,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        AngleSharp.Dom.IElement valueLink = cut.Find("a.esc-value");
        valueLink.GetAttribute("href").ShouldBe("/meets/nationals-2024");
        valueLink.TextContent.Trim().ShouldBe("220,5kg");
    }

    [Fact]
    public void WhenNonStandard_AndAthleteSlugIsNull_RendersAthleteNameAsPlainText()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: null,
            BirthYear: null,
            BodyWeight: null,
            Weight: 200m,
            Date: new DateOnly(2024, 1, 1),
            MeetSlug: null,
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
        cut.Find("span.esc-name").TextContent.ShouldBe("Jón Jónsson");
    }

    [Fact]
    public void WhenNonStandard_AndAthleteIsNull_RendersHyphenAsName()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: null,
            AthleteSlug: null,
            BirthYear: null,
            BodyWeight: null,
            Weight: 200m,
            Date: new DateOnly(2024, 1, 1),
            MeetSlug: null,
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.Find("span.esc-name").TextContent.ShouldBe("-");
    }

    [Fact]
    public void WhenNonStandard_AndMeetSlugIsNull_RendersWeightAsPlainValue()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: null,
            BodyWeight: null,
            Weight: 200m,
            Date: new DateOnly(2024, 1, 1),
            MeetSlug: null,
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.FindAll("a.esc-value").Count.ShouldBe(0);
        cut.Find("span.esc-value").TextContent.Trim().ShouldBe("200,0kg");
    }

    [Fact]
    public void WhenNonStandard_AndBirthYearIsAbsent_OmitsBirthYearPill()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: null,
            BodyWeight: 82.5m,
            Weight: 200m,
            Date: new DateOnly(2024, 1, 1),
            MeetSlug: null,
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        System.Collections.Generic.IReadOnlyList<AngleSharp.Dom.IElement> pills = cut.FindAll(".esc-pill");
        pills.Count.ShouldBe(1);
        pills[0].TextContent.ShouldBe("82,50 kg");
    }

    [Fact]
    public void WhenNonStandard_AndBodyWeightIsAbsent_OmitsBodyWeightPill()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: null,
            Weight: 200m,
            Date: new DateOnly(2024, 1, 1),
            MeetSlug: null,
            IsStandard: false);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        System.Collections.Generic.IReadOnlyList<AngleSharp.Dom.IElement> pills = cut.FindAll(".esc-pill");
        pills.Count.ShouldBe(1);
        pills[0].TextContent.ShouldBe("1990");
    }

    [Fact]
    public void WhenStandard_RendersBadgeWithWeightCategory()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: true);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.Find(".esc-leading--badge").TextContent.ShouldBe("83");
    }

    [Fact]
    public void WhenStandard_RendersBadgeAsPlainSpanWithNoHistoryLink()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: true);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.Find("span.esc-leading--badge").ShouldNotBeNull();
        cut.FindAll("a.esc-leading--badge").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenStandard_RendersIslenskiStadallinnAsPlainText()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: true);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
        cut.Find("span.esc-name").TextContent.ShouldBe("Íslenski staðallinn");
    }

    [Fact]
    public void WhenStandard_HasNoPills()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: true);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.FindAll(".esc-pill").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenStandard_RendersWeightAsPlainValue()
    {
        // Arrange
        RecordEntry entry = new(
            Id: 1,
            WeightCategory: "83",
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            BirthYear: 1990,
            BodyWeight: 82.5m,
            Weight: 220.5m,
            Date: new DateOnly(2024, 3, 15),
            MeetSlug: "nationals-2024",
            IsStandard: true);

        // Act
        IRenderedComponent<RecordCard> cut = _context.Render<RecordCard>(
            parameters => parameters.Add(p => p.Record, entry));

        // Assert
        cut.FindAll("a.esc-value").Count.ShouldBe(0);
        cut.Find("span.esc-value").TextContent.Trim().ShouldBe("220,5kg");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

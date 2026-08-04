using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes.Components;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthleteRecordCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenRendered_WeightCategoryRendersAsNonLinkingBadge()
    {
        // Arrange
        AthleteRecord record = BuildRecord(weightCategory: "93 kg");
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement badge = cut.Find("span.esc-leading--badge");
        badge.TextContent.Trim().ShouldBe("93 kg");
        cut.FindAll("a.esc-leading--badge").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenRendered_DisciplineRendersAsNonLinkingName()
    {
        // Arrange
        AthleteRecord record = BuildRecord(type: Constants.Bench);
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement name = cut.Find("span.esc-name");
        name.TextContent.Trim().ShouldBe(Constants.Bench);
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenRendered_AgeCategoryPillRendersWithCorrectText()
    {
        // Arrange
        AthleteRecord record = BuildRecord(ageCategory: "Opinn flokkur");
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement agePill = cut.Find(".esc-pill--age");
        agePill.TextContent.Trim().ShouldBe("Opinn flokkur");
    }

    [Fact]
    public void WhenAgeCategoryIsMasters_PillRendersIcelandicLabel()
    {
        // Arrange
        AthleteRecord record = BuildRecord(ageCategory: "Öldungaflokkur 1");
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement agePill = cut.Find(".esc-pill--age");
        agePill.TextContent.Trim().ShouldBe("Öldungaflokkur 1");
    }

    [Fact]
    public void WhenAgeCategoryIsMaleSubJunior_PillRendersDrengjaflokkur()
    {
        // Arrange
        AthleteRecord record = BuildRecord(ageCategory: "Drengjaflokkur");
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement agePill = cut.Find(".esc-pill--age");
        agePill.TextContent.Trim().ShouldBe("Drengjaflokkur");
    }

    [Fact]
    public void WhenRendered_DateRendersAsMetaInDdMMyyyyFormat()
    {
        // Arrange
        AthleteRecord record = BuildRecord(date: new DateOnly(2024, 3, 15));
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.Find(".esc-meta").TextContent.Trim().ShouldBe("15.03.2024");
    }

    [Fact]
    public void WhenRendered_WeightRendersAsValueLinkWithKgUnitAndAriaLabel()
    {
        // Arrange
        AthleteRecord record = BuildRecord(
            type: Constants.Squat,
            weight: 250.5m,
            meetSlug: "islandsmot-2024",
            meet: "Íslandsmót 2024");
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement valueLink = cut.Find("a.esc-value");
        valueLink.GetAttribute("href").ShouldBe("/meets/islandsmot-2024");
        valueLink.GetAttribute("aria-label").ShouldBe($"{Constants.Squat} — 250,5 kg — Íslandsmót 2024");
        valueLink.TextContent.Trim().ShouldContain("250,5");
        cut.Find(".esc-unit").TextContent.Trim().ShouldBe("kg");
    }

    [Fact]
    public void WhenRendered_WeightFormatsWithF1IcelandicCulture()
    {
        // Arrange
        AthleteRecord record = BuildRecord(weight: 200m);
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement valueLink = cut.Find("a.esc-value");
        valueLink.TextContent.Trim().ShouldContain("200,0");
    }

    [Fact]
    public void WhenIsSingleLift_SingleLiftPillRendersWithCorrectText()
    {
        // Arrange
        AthleteRecord record = BuildRecord(isSingleLift: true);
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement slPill = cut.Find(".esc-pill--single-lift");
        slPill.TextContent.Trim().ShouldBe(Constants.SingeLift);
    }

    [Fact]
    public void WhenIsNotSingleLift_SingleLiftPillIsAbsent()
    {
        // Arrange
        AthleteRecord record = BuildRecord(isSingleLift: false);
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.FindAll(".esc-pill--single-lift").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenIsSingleLift_AgePillRendersBeforeSingleLiftPill()
    {
        // Arrange
        AthleteRecord record = BuildRecord(ageCategory: "junior", isSingleLift: true);
        RegisterServices();

        // Act
        IRenderedComponent<AthleteRecordCard> cut = _context.Render<AthleteRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        IReadOnlyList<AngleSharp.Dom.IElement> pills = cut.FindAll(".esc-pill");
        pills.Count.ShouldBe(2);
        pills[0].ClassList.ShouldContain("esc-pill--age");
        pills[1].ClassList.ShouldContain("esc-pill--single-lift");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static AthleteRecord BuildRecord(
        string weightCategory = "83 kg",
        string ageCategory = "Open",
        string type = "Hnébeygja",
        decimal weight = 250m,
        bool isSingleLift = false,
        string meetSlug = "kraftlyftingar-2024",
        string meet = "Kraftlyftingar 2024",
        DateOnly date = default)
    {
        if (date == default)
        {
            date = new DateOnly(2024, 3, 15);
        }

        return new AthleteRecord(
            Date: date,
            IsClassic: true,
            IsSingleLift: isSingleLift,
            IsWithinPowerlifting: false,
            IsStandaloneDiscipline: false,
            WeightCategory: weightCategory,
            AgeCategory: ageCategory,
            Type: type,
            Weight: weight,
            Meet: meet,
            MeetSlug: meetSlug);
    }

    private void RegisterServices()
    {
        _context.AddAuthorization();
    }
}

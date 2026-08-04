using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes.Components;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthletePersonalBestCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenDisciplineIsNone_NameIsSamanlagt()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(discipline: Discipline.None);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find("span.esc-name").TextContent.Trim().ShouldBe("Samanlagt");
    }

    [Fact]
    public void WhenDisciplineIsSquat_NameIsSquatDisplayName()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(discipline: Discipline.Squat);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find("span.esc-name").TextContent.Trim().ShouldBe(Constants.Squat);
    }

    [Fact]
    public void WhenRendered_MetaTextFormatsDateAndBodyWeight()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(
            date: new DateOnly(2024, 3, 15),
            bodyWeight: 82.5m);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find(".esc-meta").TextContent.Trim().ShouldBe("15.03.2024 · 82,50 kg");
    }

    [Fact]
    public void WhenRendered_ValueIsWeightF1WithKgUnitAndMeetHref()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(weight: 250.5m, meetSlug: "islandsmot-2024");
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        AngleSharp.Dom.IElement valueLink = cut.Find("a.esc-value");
        valueLink.GetAttribute("href").ShouldBe("/meets/islandsmot-2024");
        valueLink.TextContent.Trim().ShouldContain("250,5");
        cut.Find(".esc-unit").TextContent.Trim().ShouldBe("kg");
    }

    [Fact]
    public void WhenIsSingleLift_SingleLiftPillRendersWithCorrectText()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(isSingleLift: true);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find(".esc-pill--single-lift").TextContent.Trim().ShouldBe(Constants.SingeLift);
    }

    [Fact]
    public void WhenIsNotSingleLift_SingleLiftPillIsAbsent()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(isSingleLift: false);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.FindAll(".esc-pill--single-lift").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenMatchesIsEmpty_RecordPillIsAbsent()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches: []);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.FindAll(".esc-pill--record").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenMatchesHasOneEntry_RecordPillShowsTextWithoutCount()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches:
        [
            new AthleteRecordMatch("Opinn flokkur", "93 kg", true, false, false),
        ]);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find(".esc-pill--record").TextContent.Trim().ShouldBe("★ Íslandsmet");
    }

    [Fact]
    public void WhenMatchesHasThreeEntries_RecordPillShowsCountSuffix()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches:
        [
            new AthleteRecordMatch("Opinn flokkur", "93 kg", true, false, false),
            new AthleteRecordMatch("Öldungaflokkur 1", "93 kg", true, false, false),
            new AthleteRecordMatch("Öldungaflokkur 2", "93 kg", true, false, false),
        ]);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find(".esc-pill--record").TextContent.Trim().ShouldBe("★ Íslandsmet ×3");
    }

    [Fact]
    public void WhenMatchesHasOneEntry_RecordPillTooltipRendersAgeCategoryVerbatim()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches:
        [
            new AthleteRecordMatch("Opinn flokkur", "93 kg", true, false, false),
        ]);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        string tooltip = cut.Find(".esc-pill--record").GetAttribute("data-tooltip").ShouldNotBeNull();
        tooltip.ShouldBe("Opinn flokkur · 93 kg (kraftlyftingar)");
    }

    [Fact]
    public void WhenMatchesHasMastersLabel_TooltipRendersLabelVerbatim()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches:
        [
            new AthleteRecordMatch("Öldungaflokkur 1", "83 kg", true, false, false),
        ]);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        string tooltip = cut.Find(".esc-pill--record").GetAttribute("data-tooltip").ShouldNotBeNull();
        tooltip.ShouldBe("Öldungaflokkur 1 · 83 kg (kraftlyftingar)");
    }

    [Fact]
    public void WhenMatchIsStokGrein_TooltipShowsStokGreinSuffix()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches:
        [
            new AthleteRecordMatch("Opinn flokkur", "93 kg", false, true, false),
        ]);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        string tooltip = cut.Find(".esc-pill--record").GetAttribute("data-tooltip").ShouldNotBeNull();
        tooltip.ShouldBe("Opinn flokkur · 93 kg (stök grein)");
    }

    [Fact]
    public void WhenMatchesHasMultipleEntries_TooltipJoinsLinesByNewline()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(matches:
        [
            new AthleteRecordMatch("Opinn flokkur", "93 kg", true, false, false),
            new AthleteRecordMatch("Öldungaflokkur 1", "93 kg", true, false, false),
        ]);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        string tooltip = cut.Find(".esc-pill--record").GetAttribute("data-tooltip").ShouldNotBeNull();
        tooltip.ShouldBe("Opinn flokkur · 93 kg (kraftlyftingar)\nÖldungaflokkur 1 · 93 kg (kraftlyftingar)");
    }

    [Fact]
    public void WhenDisciplineIsNone_CardHasEscTotalCssClass()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(discipline: Discipline.None);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find("article").ClassList.ShouldContain("esc-total");
    }

    [Fact]
    public void WhenDisciplineIsLift_CardDoesNotHaveEscTotalCssClass()
    {
        // Arrange
        AthletePersonalBest best = BuildBest(discipline: Discipline.Squat);
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.Find("article").ClassList.ShouldNotContain("esc-total");
    }

    [Fact]
    public void WhenRendered_NameHasNoHrefLink()
    {
        // Arrange
        AthletePersonalBest best = BuildBest();
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
        cut.Find("span.esc-name").ShouldNotBeNull();
    }

    [Fact]
    public void WhenRendered_NoLeadingBadgeIsPresent()
    {
        // Arrange
        AthletePersonalBest best = BuildBest();
        RegisterServices();

        // Act
        IRenderedComponent<AthletePersonalBestCard> cut = _context.Render<AthletePersonalBestCard>(
            parameters => parameters.Add(p => p.Best, best));

        // Assert
        cut.FindAll(".esc-leading").Count.ShouldBe(0);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static AthletePersonalBest BuildBest(
        bool isClassic = true,
        bool isSingleLift = false,
        Discipline discipline = Discipline.Squat,
        decimal weight = 200m,
        string weightCategory = "83 kg",
        decimal bodyWeight = 80m,
        string meetSlug = "kraftlyftingar-2024",
        string meetType = "Kraftlyftingar",
        DateOnly date = default,
        IReadOnlyList<AthleteRecordMatch>? matches = null)
    {
        if (date == default)
        {
            date = new DateOnly(2024, 3, 15);
        }

        return new AthletePersonalBest(
            IsClassic: isClassic,
            IsSingleLift: isSingleLift,
            Discipline: discipline,
            Weight: weight,
            WeightCategory: weightCategory,
            BodyWeight: bodyWeight,
            MeetSlug: meetSlug,
            MeetType: meetType,
            Date: date,
            Matches: matches ?? []);
    }

    private void RegisterServices()
    {
        _context.AddAuthorization();
    }
}

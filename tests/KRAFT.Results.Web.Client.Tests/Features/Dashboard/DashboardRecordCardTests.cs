using Bunit;

using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Web.Client.Features.Dashboard;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Dashboard;

public sealed class DashboardRecordCardTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenRendered_MeetDateRendersAsMetaInMMMyyyyFormat()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(meetDate: new DateOnly(2024, 3, 15));
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.Find(".esc-meta").TextContent.Trim().ShouldBe("mar. 2024");
    }

    [Fact]
    public void WhenRendered_WeightRendersAsValueLinkWithUnitHrefAndAriaLabel()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(
            lift: "Hnéböð",
            athleteName: "Jón Jónsson",
            weight: 250m,
            meetSlug: "kraftlyftingar-2024");
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement valueLink = cut.Find("a.esc-value");
        valueLink.GetAttribute("href").ShouldBe("/meets/kraftlyftingar-2024");
        valueLink.GetAttribute("aria-label").ShouldBe("Jón Jónsson — Hnéböð — 250,0 kg");
        valueLink.TextContent.Trim().ShouldContain("250,0");
        cut.Find(".esc-unit").TextContent.Trim().ShouldBe("kg");
    }

    [Fact]
    public void WhenRendered_LiftNameRendersAsLabel()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(lift: "Bekkjarlyft");
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.Find(".esc-label").TextContent.ShouldBe("Bekkjarlyft");
    }

    [Fact]
    public void WhenRendered_AgePillTextEqualsToAgeCategoryLabel()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(ageCategory: "masters1");
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement pill = cut.Find(".esc-pill--age");
        pill.TextContent.Trim().ShouldBe("Öldungaflokkur 1");
    }

    [Fact]
    public void WhenIsClassicIsTrue_EquipmentPillHasClassicClassAndText()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(isClassic: true);
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement pill = cut.Find(".esc-pill--equipment-classic");
        pill.TextContent.Trim().ShouldBe("Án búnaðar");
    }

    [Fact]
    public void WhenIsClassicIsFalse_EquipmentPillHasEquippedClassAndText()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(isClassic: false);
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement pill = cut.Find(".esc-pill--equipment-equipped");
        pill.TextContent.Trim().ShouldBe("Með búnaði");
    }

    [Fact]
    public void WhenRendered_ThreePillsInOrderWeightAgeEquipment()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord(weightCategory: "-83", ageCategory: "open", isClassic: true);
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        IReadOnlyList<AngleSharp.Dom.IElement> pills = cut.FindAll(".esc-pill");
        pills.Count.ShouldBe(3);
        pills[0].TextContent.Trim().ShouldBe("-83");
        pills[1].TextContent.Trim().ShouldBe("Opinn flokkur");
        pills[2].TextContent.Trim().ShouldBe("Án búnaðar");
    }

    [Fact]
    public void WhenRendered_NoLeadingOrImageRendered()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord();
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        cut.FindAll(".esc-leading").Count.ShouldBe(0);
        cut.FindAll(".esc-image").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenRendered_AthleteNameRendersAsLinkToAthleteSlug()
    {
        // Arrange
        DashboardRecordEntry record = BuildRecord();
        RegisterServices();

        // Act
        IRenderedComponent<DashboardRecordCard> cut = _context.Render<DashboardRecordCard>(
            parameters => parameters.Add(p => p.Record, record));

        // Assert
        AngleSharp.Dom.IElement link = cut.Find("a.esc-name");
        link.GetAttribute("href").ShouldBe("/athletes/jon-jonsson");
        link.TextContent.ShouldBe("Jón Jónsson");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static DashboardRecordEntry BuildRecord(
        string lift = "Hnéböð",
        string athleteSlug = "jon-jonsson",
        string athleteName = "Jón Jónsson",
        string weightCategory = "-83",
        string ageCategory = "open",
        bool isClassic = true,
        decimal weight = 250m,
        string meetSlug = "kraftlyftingar-2024",
        DateOnly meetDate = default)
    {
        if (meetDate == default)
        {
            meetDate = new DateOnly(2024, 3, 15);
        }

        return new DashboardRecordEntry(
            lift,
            athleteSlug,
            athleteName,
            weightCategory,
            ageCategory,
            isClassic,
            weight,
            meetSlug,
            meetDate);
    }

    private void RegisterServices()
    {
        _context.AddAuthorization();
    }
}

using Bunit;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class MeetRecordsSectionTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void WhenAgeCategoryIsMasters3_MetaLineRendersIcelandicLabelVerbatim()
    {
        // Arrange
        IReadOnlyList<MeetRecordEntry> records =
        [
            BuildRecord(ageCategory: "Öldungaflokkur 3"),
        ];

        // Act
        IRenderedComponent<MeetRecordsSection> cut = _context.Render<MeetRecordsSection>(
            parameters => parameters.Add(p => p.Records, records));

        // Assert
        AngleSharp.Dom.IElement meta = cut.Find(".mr-meta");
        meta.TextContent.Trim().ShouldBe("Öldungaflokkur 3");
    }

    [Fact]
    public void WhenAgeCategoryIsOpen_MetaLineRendersIcelandicLabelVerbatim()
    {
        // Arrange
        IReadOnlyList<MeetRecordEntry> records =
        [
            BuildRecord(ageCategory: "Opinn flokkur"),
        ];

        // Act
        IRenderedComponent<MeetRecordsSection> cut = _context.Render<MeetRecordsSection>(
            parameters => parameters.Add(p => p.Records, records));

        // Assert
        AngleSharp.Dom.IElement meta = cut.Find(".mr-meta");
        meta.TextContent.Trim().ShouldBe("Opinn flokkur");
    }

    [Fact]
    public void WhenMultipleRecords_EachMetaLineRendersItsAgeCategoryVerbatim()
    {
        // Arrange
        IReadOnlyList<MeetRecordEntry> records =
        [
            BuildRecord(ageCategory: "Opinn flokkur"),
            BuildRecord(ageCategory: "Öldungaflokkur 1"),
        ];

        // Act
        IRenderedComponent<MeetRecordsSection> cut = _context.Render<MeetRecordsSection>(
            parameters => parameters.Add(p => p.Records, records));

        // Assert
        System.Collections.Generic.IReadOnlyList<AngleSharp.Dom.IElement> metas = cut.FindAll(".mr-meta");
        metas.Count.ShouldBe(2);
        metas[0].TextContent.Trim().ShouldBe("Opinn flokkur");
        metas[1].TextContent.Trim().ShouldBe("Öldungaflokkur 1");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static MeetRecordEntry BuildRecord(
        string athleteName = "Jón Jónsson",
        string athleteSlug = "jon-jonsson",
        string discipline = "Hnébeygja",
        string weightCategory = "83",
        string ageCategory = "Opinn flokkur",
        decimal weight = 200m,
        bool isClassic = true)
    {
        return new MeetRecordEntry(
            AthleteName: athleteName,
            AthleteSlug: athleteSlug,
            Discipline: discipline,
            WeightCategory: weightCategory,
            AgeCategory: ageCategory,
            Weight: weight,
            IsClassic: isClassic);
    }
}

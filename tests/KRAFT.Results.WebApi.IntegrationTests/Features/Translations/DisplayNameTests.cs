using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Translations;

public sealed class DisplayNameTests
{
    [Theory]
    [InlineData(Discipline.Squat, "Hnébeygja")]
    [InlineData(Discipline.Bench, "Bekkpressa")]
    [InlineData(Discipline.Deadlift, "Réttstöðulyfta")]
    [InlineData(Discipline.None, "")]
    public void Discipline_ToDisplayName_ReturnsIcelandicName(Discipline discipline, string expected)
    {
        // Arrange

        // Act
        string result = discipline.ToDisplayName();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(Discipline.Squat, "H")]
    [InlineData(Discipline.Bench, "B")]
    [InlineData(Discipline.Deadlift, "R")]
    [InlineData(Discipline.None, "")]
    public void Discipline_ToAbbreviation_ReturnsSingleChar(Discipline discipline, string expected)
    {
        // Arrange

        // Act
        string result = discipline.ToAbbreviation();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("m", "Karlar")]
    [InlineData("f", "Konur")]
    [InlineData("M", "Karlar")]
    [InlineData("F", "Konur")]
    [InlineData("unknown", "")]
    public void ToGenderGroupLabel_ReturnsIcelandicPluralLabel(string gender, string expected)
    {
        // Arrange

        // Act
        string result = gender.ToGenderGroupLabel();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("m", "Karl")]
    [InlineData("f", "Kona")]
    [InlineData("M", "Karl")]
    [InlineData("F", "Kona")]
    [InlineData("unknown", "")]
    public void ToGenderSingularLabel_ReturnsIcelandicSingularLabel(string gender, string expected)
    {
        // Arrange

        // Act
        string result = gender.ToGenderSingularLabel();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, "Án búnaðar")]
    [InlineData(false, "Með búnaði")]
    public void EquipmentType_ReturnsIcelandicLabel(bool isClassic, string expected)
    {
        // Arrange

        // Act
        string result = DisplayNames.EquipmentType(isClassic);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("open", null, "Opinn flokkur")]
    [InlineData("subjunior", "m", "Drengjaflokkur")]
    [InlineData("subjunior", "f", "Stúlknaflokkur")]
    [InlineData("junior", null, "Unglingaflokkur")]
    [InlineData("masters1", null, "Öldungaflokkur 1")]
    [InlineData("masters2", null, "Öldungaflokkur 2")]
    [InlineData("masters3", null, "Öldungaflokkur 3")]
    [InlineData("masters4", null, "Öldungaflokkur 4")]
    [InlineData("unknown", null, "")]
    [InlineData("OPEN", null, "Opinn flokkur")]
    [InlineData("Junior", null, "Unglingaflokkur")]
    [InlineData("Masters2", null, "Öldungaflokkur 2")]
    [InlineData("SUBJUNIOR", "M", "Drengjaflokkur")]
    public void ToAgeCategoryLabel_ReturnsIcelandicLabel(string slug, string? gender, string expected)
    {
        // Arrange

        // Act
        string result = slug.ToAgeCategoryLabel(gender);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("open", "")]
    [InlineData("subjunior", "14-18 ára")]
    [InlineData("junior", "19-23 ára")]
    [InlineData("masters1", "40+")]
    [InlineData("masters2", "50+")]
    [InlineData("masters3", "60+")]
    [InlineData("masters4", "70+")]
    [InlineData("unknown", "")]
    [InlineData("OPEN", "")]
    [InlineData("Junior", "19-23 ára")]
    [InlineData("Masters2", "50+")]
    [InlineData("SUBJUNIOR", "14-18 ára")]
    public void ToAgeCategoryAgeRange_ReturnsAgeRange(string slug, string expected)
    {
        // Arrange

        // Act
        string result = slug.ToAgeCategoryAgeRange();

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "Hnébeygja")]
    [InlineData(2, "Bekkpressa")]
    [InlineData(3, "Réttstöðulyfta")]
    [InlineData(4, "Samtala")]
    [InlineData(5, "Bekkpressa (stök grein)")]
    [InlineData(6, "Réttstöðulyfta (stök grein)")]
    [InlineData(0, "")]
    public void RecordCategory_ToDisplayName_ReturnsIcelandicName(int categoryValue, string expected)
    {
        // Arrange
        RecordCategory category = (RecordCategory)categoryValue;

        // Act
        string result = category.ToDisplayName();

        // Assert
        result.ShouldBe(expected);
    }
}
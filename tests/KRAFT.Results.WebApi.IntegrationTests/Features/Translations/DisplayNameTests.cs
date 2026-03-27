using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Translations;

public sealed class DisplayNameTests
{
    [Theory]
    [InlineData(Discipline.Squat, "Hn\u00e9beygja")]
    [InlineData(Discipline.Bench, "Bekkpressa")]
    [InlineData(Discipline.Deadlift, "R\u00e9ttst\u00f6\u00f0ulyfta")]
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
    [InlineData(true, "\u00c1n b\u00fana\u00f0ar")]
    [InlineData(false, "Me\u00f0 b\u00fana\u00f0i")]
    public void EquipmentType_ReturnsIcelandicLabel(bool isClassic, string expected)
    {
        // Arrange

        // Act
        string result = DisplayNames.EquipmentType(isClassic);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, "Hn\u00e9beygja")]
    [InlineData(2, "Bekkpressa")]
    [InlineData(3, "R\u00e9ttst\u00f6\u00f0ulyfta")]
    [InlineData(4, "Samtala")]
    [InlineData(5, "Bekkpressa (st\u00f6k grein)")]
    [InlineData(6, "R\u00e9ttst\u00f6\u00f0ulyfta (st\u00f6k grein)")]
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
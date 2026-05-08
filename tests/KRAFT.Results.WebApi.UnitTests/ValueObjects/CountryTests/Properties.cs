using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.CountryTests;

public sealed class Properties
{
    [Fact]
    public void Alpha2_ReturnsExpectedCode()
    {
        // Arrange
        Country country = Country.Parse("ISL");

        // Act & Assert
        country.Alpha2.ShouldBe("IS");
    }

    [Fact]
    public void EnglishName_ReturnsExpectedName()
    {
        // Arrange
        Country country = Country.Parse("ISL");

        // Act & Assert
        country.EnglishName.ShouldBe("Iceland");
    }

    [Fact]
    public void IcelandicName_ReturnsIcelandicName_WhenCodeIsInDictionary()
    {
        // Arrange
        Country country = Country.Parse("ISL");

        // Act & Assert
        country.IcelandicName.ShouldBe("Ísland");
    }

    [Fact]
    public void IcelandicName_FallsBackToEnglishName_WhenNotInDictionary()
    {
        // Arrange
        // Australia is not in the Icelandic dictionary
        Country country = Country.Parse("AUS");

        // Act & Assert
        country.IcelandicName.ShouldBe(country.EnglishName);
    }

    [Fact]
    public void DisplayName_ReturnsIcelandicName()
    {
        // Arrange
        Country country = Country.Parse("ISL");

        // Act & Assert
        country.DisplayName.ShouldBe("Ísland");
    }

    [Theory]
    [InlineData("ISL", "IS", "Iceland", "Ísland")]
    [InlineData("NOR", "NO", "Norway", "Noregur")]
    [InlineData("DNK", "DK", "Denmark", "Danmörk")]
    [InlineData("USA", "US", "United States", "Bandaríkin")]
    [InlineData("DEU", "DE", "Germany", "Þýskaland")]
    public void AllProperties_AreCorrect_ForKnownCodes(
        string alpha3, string expectedAlpha2, string expectedEnglish, string expectedIcelandic)
    {
        // Arrange
        Country country = Country.Parse(alpha3);

        // Act & Assert
        country.ShouldSatisfyAllConditions(
            () => country.Alpha3.ShouldBe(alpha3),
            () => country.Alpha2.ShouldBe(expectedAlpha2),
            () => country.EnglishName.ShouldBe(expectedEnglish),
            () => country.IcelandicName.ShouldBe(expectedIcelandic),
            () => country.DisplayName.ShouldBe(expectedIcelandic));
    }
}
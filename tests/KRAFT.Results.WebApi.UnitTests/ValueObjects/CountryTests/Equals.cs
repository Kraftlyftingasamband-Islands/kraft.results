using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.CountryTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        // Arrange
        Country country = Country.Parse("ISL");

        // Act & Assert
        country.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        // Arrange
        Country country = Country.Parse("ISL");

        // Act & Assert
        country.Equals(country).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameCode()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country b = Country.Parse("ISL");

        // Act & Assert
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentCode()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country b = Country.Parse("NOR");

        // Act & Assert
        a.Equals(b).ShouldBeFalse();
    }
}
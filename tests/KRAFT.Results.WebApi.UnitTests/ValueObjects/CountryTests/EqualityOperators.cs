using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.CountryTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameCode()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country b = Country.Parse("ISL");

        // Act & Assert
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentCode()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country b = Country.Parse("NOR");

        // Act & Assert
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void EqualOperator_ReturnsFalse_WhenOneIsNull()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country? b = null;

        // Act & Assert
        (a == b).ShouldBeFalse();
    }
}
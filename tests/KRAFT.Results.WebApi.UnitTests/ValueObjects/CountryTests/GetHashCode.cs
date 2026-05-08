using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.CountryTests;

public sealed class GetHashCode
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameCode()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country b = Country.Parse("ISL");

        // Act & Assert
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void ReturnsDifferentHashCode_WhenDifferentCode()
    {
        // Arrange
        Country a = Country.Parse("ISL");
        Country b = Country.Parse("NOR");

        // Act & Assert
        a.GetHashCode().ShouldNotBe(b.GetHashCode());
    }
}
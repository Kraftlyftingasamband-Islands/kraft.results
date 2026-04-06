using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.GenderTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameValue()
    {
        // Arrange
        Gender a = Gender.Male;
        Gender b = Gender.Parse("m");

        // Act & Assert
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentValue()
    {
        // Act & Assert
        (Gender.Male != Gender.Female).ShouldBeTrue();
    }
}
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.IpfPointsTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameValue()
    {
        // Arrange
        IpfPoints a = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);

        // Act & Assert
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentValue()
    {
        // Arrange
        IpfPoints a = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 300m);

        // Act & Assert
        (a != b).ShouldBeTrue();
    }
}
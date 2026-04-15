using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.IpfPointsTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        // Arrange
        IpfPoints points = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 200m);

        // Act & Assert
        points.Equals(points).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        // Arrange
        IpfPoints a = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 200m);

        // Act & Assert
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        // Arrange
        IpfPoints a = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 300m);

        // Act & Assert
        a.Equals(b).ShouldBeFalse();
    }
}
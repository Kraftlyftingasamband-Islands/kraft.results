using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.IpfPointsTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        IpfPoints points = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);

        points.Equals(points).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        IpfPoints a = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        IpfPoints a = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 300m);

        a.Equals(b).ShouldBeFalse();
    }
}
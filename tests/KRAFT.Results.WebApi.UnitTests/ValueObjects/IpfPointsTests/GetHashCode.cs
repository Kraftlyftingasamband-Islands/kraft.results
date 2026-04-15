using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.IpfPointsTests;

public sealed class GetHashCode
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameValue()
    {
        // Arrange
        IpfPoints a = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, MeetCategory.Powerlifting, 83m, 200m);

        // Act & Assert
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
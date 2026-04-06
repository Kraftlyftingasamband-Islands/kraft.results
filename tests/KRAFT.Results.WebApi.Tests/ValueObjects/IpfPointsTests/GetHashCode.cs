using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Tests.ValueObjects.IpfPointsTests;

public sealed class GetHashCode
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameValue()
    {
        IpfPoints a = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);
        IpfPoints b = IpfPoints.Create(true, Gender.Male, "Powerlifting", 83m, 200m);

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
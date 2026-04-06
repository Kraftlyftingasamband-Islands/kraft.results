using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.GenderTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        Gender.Male.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        Gender.Male.Equals(Gender.Male).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        Gender a = Gender.Male;
        Gender b = Gender.Parse("m");

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        Gender.Male.Equals(Gender.Female).ShouldBeFalse();
    }
}
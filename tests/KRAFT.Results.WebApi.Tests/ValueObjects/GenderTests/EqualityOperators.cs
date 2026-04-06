using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Tests.ValueObjects.GenderTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameValue()
    {
        Gender a = Gender.Male;
        Gender b = Gender.Parse("m");

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentValue()
    {
        (Gender.Male != Gender.Female).ShouldBeTrue();
    }
}
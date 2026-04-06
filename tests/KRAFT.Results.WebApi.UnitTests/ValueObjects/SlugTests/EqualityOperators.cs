using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.SlugTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameValue()
    {
        Slug a = Slug.Create("hello-world");
        Slug b = Slug.Create("hello-world");

        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentValue()
    {
        Slug a = Slug.Create("hello");
        Slug b = Slug.Create("world");

        (a != b).ShouldBeTrue();
    }
}
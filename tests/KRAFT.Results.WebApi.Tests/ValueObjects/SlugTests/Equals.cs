using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Tests.ValueObjects.SlugTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        Slug slug = Slug.Create("hello-world");

        slug.Equals(slug).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        Slug a = Slug.Create("hello-world");
        Slug b = Slug.Create("hello-world");

        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        Slug a = Slug.Create("hello");
        Slug b = Slug.Create("world");

        a.Equals(b).ShouldBeFalse();
    }
}
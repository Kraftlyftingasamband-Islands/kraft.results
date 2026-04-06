using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.SlugTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        // Arrange
        Slug slug = Slug.Create("hello-world");

        // Act & Assert
        slug.Equals(slug).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        // Arrange
        Slug a = Slug.Create("hello-world");
        Slug b = Slug.Create("hello-world");

        // Act & Assert
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        // Arrange
        Slug a = Slug.Create("hello");
        Slug b = Slug.Create("world");

        // Act & Assert
        a.Equals(b).ShouldBeFalse();
    }
}
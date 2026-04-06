using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.ValueObjects;

public sealed class SlugGetHashCodeTests
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameValue()
    {
        Slug a = Slug.Create("hello-world");
        Slug b = Slug.Create("hello-world");

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
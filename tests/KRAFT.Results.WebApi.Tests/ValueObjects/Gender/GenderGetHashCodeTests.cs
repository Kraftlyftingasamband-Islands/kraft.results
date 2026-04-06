using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.ValueObjects;

public sealed class GenderGetHashCodeTests
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameValue()
    {
        Gender a = Gender.Male;
        Gender b = Gender.Parse("m");

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
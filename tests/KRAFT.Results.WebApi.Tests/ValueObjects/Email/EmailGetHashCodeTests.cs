using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.ValueObjects;

public sealed class EmailGetHashCodeTests
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameValue()
    {
        Email a = Email.Create("test@example.com").FromResult();
        Email b = Email.Create("test@example.com").FromResult();

        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
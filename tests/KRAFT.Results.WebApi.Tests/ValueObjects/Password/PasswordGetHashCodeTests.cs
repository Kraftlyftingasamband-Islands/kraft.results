using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.ValueObjects;

public sealed class PasswordGetHashCodeTests
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameHashedValue()
    {
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        password.GetHashCode().ShouldBe(same.GetHashCode());
    }
}
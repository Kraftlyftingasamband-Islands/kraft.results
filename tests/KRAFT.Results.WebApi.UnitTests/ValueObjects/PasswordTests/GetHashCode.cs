using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.PasswordTests;

public sealed class GetHashCode
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameHashedValue()
    {
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        password.GetHashCode().ShouldBe(same.GetHashCode());
    }
}
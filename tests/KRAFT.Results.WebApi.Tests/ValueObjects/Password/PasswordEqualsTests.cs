using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.ValueObjects;

public sealed class PasswordEqualsTests
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        Password password = Password.Hash("secret123").FromResult();

        password.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        Password password = Password.Hash("secret123").FromResult();

        password.Equals(password).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameHashedValue()
    {
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        password.Equals(same).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentHashedValues()
    {
        Password a = Password.Hash("secret123").FromResult();
        Password b = Password.Hash("secret456").FromResult();

        a.Equals(b).ShouldBeFalse();
    }
}
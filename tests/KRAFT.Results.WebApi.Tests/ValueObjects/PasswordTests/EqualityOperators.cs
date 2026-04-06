using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.Tests.ValueObjects.PasswordTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameHashedValue()
    {
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        (password == same).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentHashedValues()
    {
        Password a = Password.Hash("secret123").FromResult();
        Password b = Password.Hash("secret456").FromResult();

        (a != b).ShouldBeTrue();
    }
}
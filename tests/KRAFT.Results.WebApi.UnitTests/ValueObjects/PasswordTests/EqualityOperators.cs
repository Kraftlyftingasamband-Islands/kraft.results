using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.PasswordTests;

public sealed class EqualityOperators
{
    [Fact]
    public void EqualOperator_ReturnsTrue_WhenSameHashedValue()
    {
        // Arrange
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        // Act & Assert
        (password == same).ShouldBeTrue();
    }

    [Fact]
    public void InequalityOperator_ReturnsTrue_WhenDifferentHashedValues()
    {
        // Arrange
        Password a = Password.Hash("secret123").FromResult();
        Password b = Password.Hash("secret456").FromResult();

        // Act & Assert
        (a != b).ShouldBeTrue();
    }
}
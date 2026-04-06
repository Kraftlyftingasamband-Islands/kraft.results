using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.PasswordTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        // Arrange
        Password password = Password.Hash("secret123").FromResult();

        // Act & Assert
        password.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        // Arrange
        Password password = Password.Hash("secret123").FromResult();

        // Act & Assert
        password.Equals(password).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameHashedValue()
    {
        // Arrange
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        // Act & Assert
        password.Equals(same).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentHashedValues()
    {
        // Arrange
        Password a = Password.Hash("secret123").FromResult();
        Password b = Password.Hash("secret456").FromResult();

        // Act & Assert
        a.Equals(b).ShouldBeFalse();
    }
}
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.EmailTests;

public sealed class Equals
{
    [Fact]
    public void ReturnsFalse_WhenOtherIsNull()
    {
        // Arrange
        Email email = Email.Create("test@example.com").FromResult();

        // Act & Assert
        email.Equals(null).ShouldBeFalse();
    }

    [Fact]
    public void ReturnsTrue_WhenSameInstance()
    {
        // Arrange
        Email email = Email.Create("test@example.com").FromResult();

        // Act & Assert
        email.Equals(email).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsTrue_WhenSameValue()
    {
        // Arrange
        Email a = Email.Create("test@example.com").FromResult();
        Email b = Email.Create("test@example.com").FromResult();

        // Act & Assert
        a.Equals(b).ShouldBeTrue();
    }

    [Fact]
    public void ReturnsFalse_WhenDifferentValue()
    {
        // Arrange
        Email a = Email.Create("a@example.com").FromResult();
        Email b = Email.Create("b@example.com").FromResult();

        // Act & Assert
        a.Equals(b).ShouldBeFalse();
    }
}
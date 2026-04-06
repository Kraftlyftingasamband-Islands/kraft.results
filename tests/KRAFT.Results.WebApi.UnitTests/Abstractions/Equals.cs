using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Abstractions;

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
    public void ReturnsFalse_WhenComparingDifferentSubtypesWithSameUnderlyingValue()
    {
        // Arrange
        Gender gender = Gender.Male;
        Slug slug = Slug.Create("m");

        // Act
        bool result = gender.Equals(slug);

        // Assert
        result.ShouldBeFalse();
    }
}
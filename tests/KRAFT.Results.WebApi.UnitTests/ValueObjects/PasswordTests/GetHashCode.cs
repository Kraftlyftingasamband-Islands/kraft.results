using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.PasswordTests;

public sealed class GetHashCode
{
    [Fact]
    public void ReturnsSameHashCode_WhenSameHashedValue()
    {
        // Arrange
        Password password = Password.Hash("secret123").FromResult();
        Password same = Password.Parse(password.Value);

        // Act & Assert
        password.GetHashCode().ShouldBe(same.GetHashCode());
    }
}
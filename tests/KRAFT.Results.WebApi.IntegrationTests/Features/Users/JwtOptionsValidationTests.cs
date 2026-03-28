using System.ComponentModel.DataAnnotations;

using KRAFT.Results.WebApi.Features.Users.Infrastructure;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

public sealed class JwtOptionsValidationTests
{
    [Fact]
    public void Succeeds_WhenAllPropertiesAreValid()
    {
        // Arrange
        JwtOptions options = new()
        {
            Key = "valid-key-value",
            Issuer = "valid-issuer",
            Audience = "valid-audience",
        };

        // Act
        bool isValid = TryValidate(options, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeTrue();
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Fails_WhenKeyIsEmpty()
    {
        // Arrange
        JwtOptions options = new()
        {
            Key = string.Empty,
            Issuer = "valid-issuer",
            Audience = "valid-audience",
        };

        // Act
        bool isValid = TryValidate(options, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Key)));
    }

    [Fact]
    public void Fails_WhenIssuerIsEmpty()
    {
        // Arrange
        JwtOptions options = new()
        {
            Key = "valid-key-value",
            Issuer = string.Empty,
            Audience = "valid-audience",
        };

        // Act
        bool isValid = TryValidate(options, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Issuer)));
    }

    [Fact]
    public void Fails_WhenAudienceIsEmpty()
    {
        // Arrange
        JwtOptions options = new()
        {
            Key = "valid-key-value",
            Issuer = "valid-issuer",
            Audience = string.Empty,
        };

        // Act
        bool isValid = TryValidate(options, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeFalse();
        results.ShouldContain(r => r.MemberNames.Contains(nameof(JwtOptions.Audience)));
    }

    [Fact]
    public void Fails_WhenAllPropertiesAreEmpty()
    {
        // Arrange
        JwtOptions options = new()
        {
            Key = string.Empty,
            Issuer = string.Empty,
            Audience = string.Empty,
        };

        // Act
        bool isValid = TryValidate(options, out List<ValidationResult> results);

        // Assert
        isValid.ShouldBeFalse();
        results.Count.ShouldBe(3);
    }

    private static bool TryValidate(JwtOptions options, out List<ValidationResult> results)
    {
        results = [];
        ValidationContext context = new(options);
        return Validator.TryValidateObject(options, context, results, validateAllProperties: true);
    }
}
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.CountryTests;

public sealed class FromCode
{
    [Fact]
    public void ReturnsSuccess_WhenCodeIsValid()
    {
        // Act
        Result<Country> result = Country.FromCode("ISL");

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ReturnsCountryWithAlpha3_WhenCodeIsValid()
    {
        // Act
        Country country = Country.FromCode("ISL").FromResult();

        // Assert
        country.Alpha3.ShouldBe("ISL");
    }

    [Fact]
    public void ReturnsFailure_WhenCodeIsInvalid()
    {
        // Act
        Result<Country> result = Country.FromCode("XYZ");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Country.InvalidCode");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReturnsFailure_WhenCodeIsEmptyOrWhitespace(string code)
    {
        // Act
        Result<Country> result = Country.FromCode(code);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Country.InvalidCode");
    }

    [Fact]
    public void ReturnsFailure_WhenCodeIsNull()
    {
        // Act
        Result<Country> result = Country.FromCode(null!);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Country.InvalidCode");
    }
}
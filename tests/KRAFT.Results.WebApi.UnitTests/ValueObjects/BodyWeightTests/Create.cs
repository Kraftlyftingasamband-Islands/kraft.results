using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.BodyWeightTests;

public sealed class Create
{
    [Fact]
    public void ReturnsFailure_WhenValueIsZero()
    {
        // Act
        Result<BodyWeight> result = BodyWeight.Create(0m);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.MustBePositive");
        result.Error.Description.ShouldBe("Body weight must be greater than zero.");
    }

    [Fact]
    public void ReturnsFailure_WhenValueIsNegative()
    {
        // Act
        Result<BodyWeight> result = BodyWeight.Create(-1m);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.MustBePositive");
        result.Error.Description.ShouldBe("Body weight must be greater than zero.");
    }

    [Fact]
    public void ReturnsFailure_WhenValueExceedsMaximum()
    {
        // Act
        Result<BodyWeight> result = BodyWeight.Create(400.001m);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.TooHigh");
        result.Error.Description.ShouldBe("Body weight must not exceed 400 kg.");
    }

    [Fact]
    public void ReturnsSuccess_WhenValueIsAtMaximumBoundary()
    {
        // Act
        Result<BodyWeight> result = BodyWeight.Create(400m);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ReturnsSuccess_WithCorrectValue()
    {
        // Act
        Result<BodyWeight> result = BodyWeight.Create(83.5m);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        BodyWeight bodyWeight = result.FromResult();
        bodyWeight.Value.ShouldBe(83.5m);
    }
}
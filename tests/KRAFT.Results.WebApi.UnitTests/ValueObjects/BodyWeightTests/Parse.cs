using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.ValueObjects.BodyWeightTests;

public sealed class Parse
{
    [Fact]
    public void ReturnsBodyWeight_WhenValueIsZero()
    {
        // Act
        BodyWeight bodyWeight = BodyWeight.Parse(0m);

        // Assert
        bodyWeight.Value.ShouldBe(0m);
    }

    [Fact]
    public void ReturnsBodyWeight_WhenValueIsValid()
    {
        // Act
        BodyWeight bodyWeight = BodyWeight.Parse(83.5m);

        // Assert
        bodyWeight.Value.ShouldBe(83.5m);
    }
}
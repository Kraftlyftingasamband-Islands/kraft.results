using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Features.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetCategoryExtensionsTests;

public sealed class ToDisplayName
{
    [Fact]
    public void Powerlifting_ReturnsConstantsPowerlifting()
    {
        // Arrange
        MeetCategory category = MeetCategory.Powerlifting;

        // Act
        string result = category.ToDisplayName();

        // Assert
        result.ShouldBe(Constants.Powerlifting);
    }

    [Fact]
    public void Benchpress_ReturnsBenchWithSingleLiftSuffix()
    {
        // Arrange
        MeetCategory category = MeetCategory.Benchpress;

        // Act
        string result = category.ToDisplayName();

        // Assert
        result.ShouldBe($"{Constants.Bench} ({Constants.SingeLift})");
    }

    [Fact]
    public void Deadlift_ReturnsDeadliftWithSingleLiftSuffix()
    {
        // Arrange
        MeetCategory category = MeetCategory.Deadlift;

        // Act
        string result = category.ToDisplayName();

        // Assert
        result.ShouldBe($"{Constants.Deadlift} ({Constants.SingeLift})");
    }

    [Fact]
    public void PushPull_ReturnsConstantsPushPull()
    {
        // Arrange
        MeetCategory category = MeetCategory.PushPull;

        // Act
        string result = category.ToDisplayName();

        // Assert
        result.ShouldBe(Constants.PushPull);
    }
}
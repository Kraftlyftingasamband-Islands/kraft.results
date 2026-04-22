using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.UpdateBodyWeight;

public sealed class BodyWeightValidationTests
{
    [Fact]
    public void ReturnsFailure_WhenBodyWeightIsZero()
    {
        // Arrange
        WebApi.Features.Participations.Participation participation = CreateParticipation();

        // Act
        Result result = participation.UpdateBodyWeight(0m, "testuser");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.MustBePositive");
    }

    [Fact]
    public void ReturnsFailure_WhenBodyWeightExceedsMaximum()
    {
        // Arrange
        WebApi.Features.Participations.Participation participation = CreateParticipation();

        // Act
        Result result = participation.UpdateBodyWeight(400.001m, "testuser");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.TooHigh");
    }

    [Fact]
    public void UpdatesWeight_WhenValid()
    {
        // Arrange
        WebApi.Features.Participations.Participation participation = CreateParticipation();

        // Act
        Result result = participation.UpdateBodyWeight(90.0m, "testuser");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        participation.Weight.Value.ShouldBe(90.0m);
    }

    private static WebApi.Features.Participations.Participation CreateParticipation()
    {
        User creator = new UserBuilder().Build();
        return WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();
    }
}
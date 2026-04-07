using System.Reflection;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.Create;

public sealed class BodyWeightValidationTests
{
    [Fact]
    public void ReturnsFailure_WhenBodyWeightIsZero()
    {
        // Arrange
        User creator = CreateUser("testuser");

        // Act
        Result<WebApi.Features.Participations.Participation> result =
            WebApi.Features.Participations.Participation.Create(
                creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 0m);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.MustBePositive");
    }

    [Fact]
    public void ReturnsFailure_WhenBodyWeightExceedsMaximum()
    {
        // Arrange
        User creator = CreateUser("testuser");

        // Act
        Result<WebApi.Features.Participations.Participation> result =
            WebApi.Features.Participations.Participation.Create(
                creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 400.001m);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("BodyWeight.TooHigh");
    }

    [Fact]
    public void StoresBodyWeightValueObject_WhenValid()
    {
        // Arrange
        User creator = CreateUser("testuser");

        // Act
        Result<WebApi.Features.Participations.Participation> result =
            WebApi.Features.Participations.Participation.Create(
                creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        WebApi.Features.Participations.Participation participation = result.FromResult();
        participation.Weight.ShouldBeOfType<BodyWeight>();
        participation.Weight.Value.ShouldBe(83.5m);
    }

    private static User CreateUser(string username)
    {
        User user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;
        PropertyInfo usernameProperty = typeof(User).GetProperty(nameof(User.Username))!;
        usernameProperty.SetValue(user, username);
        return user;
    }
}
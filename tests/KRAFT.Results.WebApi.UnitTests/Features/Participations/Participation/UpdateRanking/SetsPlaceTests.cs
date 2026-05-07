using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.UpdateRanking;

public sealed class SetsPlaceTests
{
    [Fact]
    public void SetsPlace_WhenCalledWithPositivePlace()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Act
        participation.UpdateRanking(1);

        // Assert
        participation.Place.ShouldBe(1);
    }

    [Fact]
    public void SetsPlaceToZero_WhenCalledWithZero()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Act
        participation.UpdateRanking(0);

        // Assert
        participation.Place.ShouldBe(0);
    }
}
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.UpdateRanking;

public sealed class SetsTeamPointsTests
{
    [Theory]
    [InlineData(1, 12)]
    [InlineData(2, 9)]
    [InlineData(3, 8)]
    [InlineData(4, 7)]
    [InlineData(5, 6)]
    [InlineData(6, 5)]
    [InlineData(7, 4)]
    [InlineData(8, 3)]
    [InlineData(9, 2)]
    [InlineData(10, 1)]
    [InlineData(11, 0)]
    [InlineData(100, 0)]
    public void SetsTeamPoints_BasedOnPlace(int place, int expectedTeamPoints)
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Act
        participation.UpdateRanking(place);

        // Assert
        participation.TeamPoints.ShouldBe(expectedTeamPoints);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetsTeamPoints_ToZero_WhenDqOrNotComputed(int place)
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Act
        participation.UpdateRanking(place);

        // Assert
        participation.TeamPoints.ShouldBe(0);
    }
}
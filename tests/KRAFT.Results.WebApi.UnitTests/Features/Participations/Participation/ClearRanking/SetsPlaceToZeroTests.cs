using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.ClearRanking;

public sealed class SetsPlaceToZeroTests
{
    [Fact]
    public void SetsPlaceToZero()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();
        participation.UpdateRanking(3);

        // Act
        participation.ClearRanking();

        // Assert
        participation.Place.ShouldBe(0);
    }
}
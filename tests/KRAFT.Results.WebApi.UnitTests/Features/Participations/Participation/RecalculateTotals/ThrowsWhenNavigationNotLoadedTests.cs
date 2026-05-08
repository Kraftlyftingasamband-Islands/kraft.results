using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;
using KRAFT.Results.WebApi.UnitTests.Helpers;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.RecalculateTotals;

public sealed class ThrowsWhenNavigationNotLoadedTests
{
    [Fact]
    public void WhenAthleteNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Act
        Action act = () => participation.RecalculateTotals();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }

    [Fact]
    public void WhenMeetNotLoaded_ThrowsInvalidOperationException()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        WebApi.Features.Athletes.Athlete athlete = WebApi.Features.Athletes.Athlete.Create(
            creator, "John", "Doe", "m", new Country(), null, null).FromResult();
        ParticipationTestHelper.SetProperty(participation, nameof(WebApi.Features.Participations.Participation.Athlete), athlete);

        // Act
        Action act = () => participation.RecalculateTotals();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}
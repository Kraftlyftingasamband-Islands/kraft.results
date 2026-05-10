using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Meets.MeetTests.Update;

public sealed class RaisesCalcPlacesChangedEventTests
{
    [Fact]
    public void RaisesCalcPlacesChangedEvent_WhenCalcPlacesChangesFromTrueToFalse()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        Meet meet = Meet.Create(
            creator,
            MeetCategory.Powerlifting,
            title: "Test Meet",
            startDate: new DateOnly(2025, 6, 1),
            calcPlaces: true).FromResult();
        int meetId = 42;

        // Act
        meet.Update(
            meetId,
            creator,
            MeetCategory.Powerlifting,
            title: "Test Meet",
            startDate: new DateOnly(2025, 6, 1),
            calcPlaces: false);

        // Assert
        CalcPlacesChangedEvent? raisedEvent = meet.DomainEvents
            .OfType<CalcPlacesChangedEvent>()
            .FirstOrDefault();
        raisedEvent.ShouldNotBeNull();
        raisedEvent.MeetId.ShouldBe(meetId);
        raisedEvent.CalcPlaces.ShouldBeFalse();
    }

    [Fact]
    public void DoesNotRaiseCalcPlacesChangedEvent_WhenCalcPlacesDoesNotChange()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        Meet meet = Meet.Create(
            creator,
            MeetCategory.Powerlifting,
            title: "Test Meet",
            startDate: new DateOnly(2025, 6, 1),
            calcPlaces: true).FromResult();
        int meetId = 42;

        // Act
        meet.Update(
            meetId,
            creator,
            MeetCategory.Powerlifting,
            title: "Test Meet",
            startDate: new DateOnly(2025, 6, 1),
            calcPlaces: true);

        // Assert
        meet.DomainEvents
            .OfType<CalcPlacesChangedEvent>()
            .ShouldBeEmpty();
    }

    [Fact]
    public void DoesNotRaiseCalcPlacesChangedEvent_WhenValidationFails()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        Meet meet = Meet.Create(
            creator,
            MeetCategory.Powerlifting,
            title: "Test Meet",
            startDate: new DateOnly(2025, 6, 1),
            calcPlaces: true).FromResult();
        int meetId = 42;

        // Act — empty title fails validation
        meet.Update(
            meetId,
            creator,
            MeetCategory.Powerlifting,
            title: string.Empty,
            startDate: new DateOnly(2025, 6, 1),
            calcPlaces: false);

        // Assert
        meet.DomainEvents
            .OfType<CalcPlacesChangedEvent>()
            .ShouldBeEmpty();
    }
}
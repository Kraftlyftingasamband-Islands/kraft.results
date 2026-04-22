using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.RecordAttempt;

public sealed class RaisesAttemptRecordedEventTests
{
    [Fact]
    public void RaisesAttemptRecordedEvent()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Act
        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 200m, good: true, createdBy: "testuser");

        // Assert
        participation.DomainEvents.Count.ShouldBe(2);
        IDomainEvent secondEvent = participation.DomainEvents.Last();
        AttemptRecordedEvent recordedEvent = secondEvent.ShouldBeOfType<AttemptRecordedEvent>();
        recordedEvent.Participation.ShouldBeSameAs(participation);
        recordedEvent.Attempt.ShouldNotBeNull();
        recordedEvent.Attempt.Discipline.ShouldBe(Discipline.Squat);
        recordedEvent.Attempt.Round.ShouldBe((short)1);
        recordedEvent.Attempt.Weight.ShouldBe(200m);
    }
}
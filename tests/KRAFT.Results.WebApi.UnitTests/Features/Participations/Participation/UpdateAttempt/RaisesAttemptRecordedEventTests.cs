using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.UpdateAttempt;

public sealed class RaisesAttemptRecordedEventTests
{
    [Fact]
    public void RaisesAttemptRecordedEvent()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 200m, good: true, createdBy: "testuser");

        Attempt attempt = participation.Attempts.Single();

        // Act
        participation.UpdateAttempt(attempt, weight: 210m, good: true, modifiedBy: "testuser");

        // Assert
        participation.DomainEvents.Count.ShouldBe(3);
        IDomainEvent lastEvent = participation.DomainEvents.Last();
        AttemptRecordedEvent recordedEvent = lastEvent.ShouldBeOfType<AttemptRecordedEvent>();
        recordedEvent.Participation.ShouldBeSameAs(participation);
        recordedEvent.Attempt.ShouldBeSameAs(attempt);
        recordedEvent.Attempt.Weight.ShouldBe(210m);
    }
}
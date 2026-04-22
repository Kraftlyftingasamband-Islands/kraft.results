using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.Create;

public sealed class RaisesParticipationAddedEventTests
{
    [Fact]
    public void RaisesExactlyOneParticipationAddedEvent()
    {
        // Arrange
        User creator = new UserBuilder().Build();

        // Act
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Assert
        participation.DomainEvents.Count.ShouldBe(1);
        IDomainEvent domainEvent = participation.DomainEvents.ShouldHaveSingleItem();
        ParticipationAddedEvent addedEvent = domainEvent.ShouldBeOfType<ParticipationAddedEvent>();
        addedEvent.Participation.ShouldBeSameAs(participation);
    }
}
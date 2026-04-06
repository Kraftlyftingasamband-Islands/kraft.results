using System.Reflection;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.Create;

public sealed class RaisesParticipationAddedEventTests
{
    [Fact]
    public void RaisesExactlyOneParticipationAddedEvent()
    {
        // Arrange
        User creator = CreateUser("testuser");

        // Act
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        // Assert
        participation.DomainEvents.Count.ShouldBe(1);
        IDomainEvent domainEvent = participation.DomainEvents.ShouldHaveSingleItem();
        ParticipationAddedEvent addedEvent = domainEvent.ShouldBeOfType<ParticipationAddedEvent>();
        addedEvent.Participation.ShouldBeSameAs(participation);
    }

    private static User CreateUser(string username)
    {
        User user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;
        PropertyInfo usernameProperty = typeof(User).GetProperty(nameof(User.Username))!;
        usernameProperty.SetValue(user, username);
        return user;
    }
}
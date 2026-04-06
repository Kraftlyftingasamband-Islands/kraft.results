using System.Reflection;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Athletes.Athlete.Create;

public sealed class RaisesAthleteCreatedEventTests
{
    [Fact]
    public void RaisesExactlyOneAthleteCreatedEvent()
    {
        // Arrange
        User creator = CreateUser("testuser");
        Country country = new();

        // Act
        WebApi.Features.Athletes.Athlete athlete = WebApi.Features.Athletes.Athlete.Create(
            creator, "John", "Doe", "m", country, null, null).FromResult();

        // Assert
        athlete.DomainEvents.Count.ShouldBe(1);
        IDomainEvent domainEvent = athlete.DomainEvents.ShouldHaveSingleItem();
        AthleteCreatedEvent createdEvent = domainEvent.ShouldBeOfType<AthleteCreatedEvent>();
        createdEvent.Athlete.ShouldBeSameAs(athlete);
    }

    private static User CreateUser(string username)
    {
        User user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;
        PropertyInfo usernameProperty = typeof(User).GetProperty(nameof(User.Username))!;
        usernameProperty.SetValue(user, username);
        return user;
    }
}
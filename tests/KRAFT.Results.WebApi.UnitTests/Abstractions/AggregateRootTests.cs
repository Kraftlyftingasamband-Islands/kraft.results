using System.Reflection;

using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Abstractions;

public sealed class AggregateRootTests
{
    [Fact]
    public void ClearDomainEvents_EmptiesEventList()
    {
        // Arrange
        User creator = CreateUser("testuser");
        Country country = new();
        Athlete athlete = Athlete.Create(creator, "John", "Doe", "m", country, null, null).FromResult();
        athlete.DomainEvents.ShouldNotBeEmpty();

        // Act
        athlete.ClearDomainEvents();

        // Assert
        athlete.DomainEvents.ShouldBeEmpty();
    }

    private static User CreateUser(string username)
    {
        User user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;
        PropertyInfo usernameProperty = typeof(User).GetProperty(nameof(User.Username))!;
        usernameProperty.SetValue(user, username);
        return user;
    }
}
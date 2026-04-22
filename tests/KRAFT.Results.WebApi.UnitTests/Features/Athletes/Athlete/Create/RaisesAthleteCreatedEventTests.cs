using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Athletes.Athlete.Create;

public sealed class RaisesAthleteCreatedEventTests
{
    [Fact]
    public void RaisesExactlyOneAthleteCreatedEvent()
    {
        // Arrange
        User creator = new UserBuilder().Build();
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
}
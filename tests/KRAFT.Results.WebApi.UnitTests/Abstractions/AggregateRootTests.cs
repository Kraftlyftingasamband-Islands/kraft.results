using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Abstractions;

public sealed class AggregateRootTests
{
    [Fact]
    public void ClearDomainEvents_EmptiesEventList()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        Country country = new();
        Athlete athlete = Athlete.Create(creator, "John", "Doe", "m", country, null, null).FromResult();
        athlete.DomainEvents.ShouldNotBeEmpty();

        // Act
        athlete.ClearDomainEvents();

        // Assert
        athlete.DomainEvents.ShouldBeEmpty();
    }
}
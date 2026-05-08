using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Athletes.Athlete.AddBan;

public sealed class AddBanTests
{
    [Fact]
    public void WhenBanAdded_AddsBanToBansCollection()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder().Build();

        // Act
        athlete.AddBan(ban);

        // Assert
        athlete.Bans.ShouldContain(ban);
    }

    [Fact]
    public void WhenBanAdded_RaisesBanAddedEvent()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();

        // Act
        athlete.AddBan(ban);

        // Assert
        BanAddedEvent? domainEvent = athlete.DomainEvents
            .OfType<BanAddedEvent>()
            .FirstOrDefault();

        domainEvent.ShouldNotBeNull();
        domainEvent.AthleteId.ShouldBe(athlete.AthleteId);
        domainEvent.FromDate.ShouldBe(ban.FromDate);
        domainEvent.ToDate.ShouldBe(ban.ToDate);
    }

    private static WebApi.Features.Athletes.Athlete CreateAthlete()
    {
        User creator = new UserBuilder().Build();
        Country country = Country.Parse("ISL");
        return WebApi.Features.Athletes.Athlete.Create(
            creator, "John", "Doe", "m", country, null, null).FromResult();
    }
}
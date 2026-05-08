using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Athletes.Athlete.HasActiveBan;

public sealed class HasActiveBanTests
{
    [Fact]
    public void WhenNoBans_ReturnsFalse()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();

        // Act
        bool result = athlete.HasActiveBan(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WhenDateEqualsBanToDate_ReturnsTrue()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.HasActiveBan(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenDateEqualsBanFromDate_ReturnsTrue()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.HasActiveBan(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenBanStartsAfterMeetDate_ReturnsFalse()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.HasActiveBan(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WhenBanEndsBeforeMeetDate_ReturnsFalse()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.HasActiveBan(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WhenBanCoversMeetDate_ReturnsTrue()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.HasActiveBan(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeTrue();
    }

    private static WebApi.Features.Athletes.Athlete CreateAthlete()
    {
        User creator = new UserBuilder().Build();
        Country country = new();
        return WebApi.Features.Athletes.Athlete.Create(
            creator, "John", "Doe", "m", country, null, null).FromResult();
    }
}
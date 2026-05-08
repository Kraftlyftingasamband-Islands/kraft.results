using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Athletes.Athlete.IsEligibleForRecord;

public sealed class IsEligibleForRecordTests
{
    [Fact]
    public void WhenNoBans_ReturnsTrue()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenBanCoversMeetDate_ReturnsFalse()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WhenBanEndsBeforeMeetDate_ReturnsTrue()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenBanStartsAfterMeetDate_ReturnsTrue()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void WhenMeetDateEqualsBanFromDate_ReturnsFalse()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void WhenMeetDateEqualsBanToDate_ReturnsFalse()
    {
        // Arrange
        WebApi.Features.Athletes.Athlete athlete = CreateAthlete();
        Ban ban = new BanBuilder()
            .WithFromDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .WithToDate(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    private static WebApi.Features.Athletes.Athlete CreateAthlete()
    {
        User creator = new UserBuilder().Build();
        Country country = Country.Parse("ISL");
        return WebApi.Features.Athletes.Athlete.Create(
            creator, "John", "Doe", "m", country, null, null).FromResult();
    }
}
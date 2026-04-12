using System.Reflection;

using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Bans;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;

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
        Ban ban = CreateBan(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));
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
        Ban ban = CreateBan(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));
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
        Ban ban = CreateBan(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc));
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
        Ban ban = CreateBan(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));
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
        Ban ban = CreateBan(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
        athlete.Bans.Add(ban);

        // Act
        bool result = athlete.IsEligibleForRecord(new DateOnly(2025, 6, 15));

        // Assert
        result.ShouldBeFalse();
    }

    private static WebApi.Features.Athletes.Athlete CreateAthlete()
    {
        User creator = CreateUser("testuser");
        Country country = new();
        return WebApi.Features.Athletes.Athlete.Create(
            creator, "John", "Doe", "m", country, null, null).FromResult();
    }

    private static User CreateUser(string username)
    {
        User user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;
        PropertyInfo usernameProperty = typeof(User).GetProperty(nameof(User.Username))!;
        usernameProperty.SetValue(user, username);
        return user;
    }

    private static Ban CreateBan(DateTime fromDate, DateTime toDate)
    {
        Ban ban = (Ban)Activator.CreateInstance(typeof(Ban), nonPublic: true)!;
        typeof(Ban).GetProperty(nameof(Ban.FromDate))!.SetValue(ban, fromDate);
        typeof(Ban).GetProperty(nameof(Ban.ToDate))!.SetValue(ban, toDate);
        return ban;
    }
}
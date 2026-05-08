using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;
using KRAFT.Results.WebApi.UnitTests.Helpers;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.RecalculateTotals;

public sealed class SetsDisqualifiedWhenBannedTests
{
    [Fact]
    public void SetsDisqualifiedTrue_WhenAthleteHasActiveBanOnMeetDate()
    {
        // Arrange
        User creator = new UserBuilder().Build();

        // Ban covers 2025-01-01 to 2025-12-31; meet is on 2025-06-15
        WebApi.Features.Athletes.Athlete athlete = CreateAthleteWithBan(
            creator,
            banFrom: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            banTo: new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        DateTime meetDate = new(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        WebApi.Features.Participations.Participation participation = ParticipationTestHelper.CreateParticipationWithNavigations(
            creator, meetDate, athlete);

        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 100m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Bench, round: 1, weight: 60m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Deadlift, round: 1, weight: 120m, good: true, createdBy: "test");

        // Act
        participation.RecalculateTotals();

        // Assert
        participation.Disqualified.ShouldBeTrue();
        participation.Total.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SetsDisqualifiedFalse_WhenAthletesBanDoesNotCoverMeetDate()
    {
        // Arrange
        User creator = new UserBuilder().Build();

        // Ban ended before meet date: ban is 2024-01-01 to 2024-12-31; meet is on 2025-06-15
        WebApi.Features.Athletes.Athlete athlete = CreateAthleteWithBan(
            creator,
            banFrom: new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            banTo: new DateTime(2024, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        DateTime meetDate = new(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        WebApi.Features.Participations.Participation participation = ParticipationTestHelper.CreateParticipationWithNavigations(
            creator, meetDate, athlete);

        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 100m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Bench, round: 1, weight: 60m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Deadlift, round: 1, weight: 120m, good: true, createdBy: "test");

        // Act
        participation.RecalculateTotals();

        // Assert
        participation.Disqualified.ShouldBeFalse();
    }

    [Fact]
    public void SetsDisqualifiedTrue_WhenAthleteHasActiveBanAndBombedOut()
    {
        // Arrange
        User creator = new UserBuilder().Build();

        // Ban covers meet date
        WebApi.Features.Athletes.Athlete athlete = CreateAthleteWithBan(
            creator,
            banFrom: new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            banTo: new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc));

        DateTime meetDate = new(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        WebApi.Features.Participations.Participation participation = ParticipationTestHelper.CreateParticipationWithNavigations(
            creator, meetDate, athlete);

        // All squat attempts failed — bomb-out
        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Squat, round: 2, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Squat, round: 3, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Bench, round: 1, weight: 60m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Deadlift, round: 1, weight: 120m, good: true, createdBy: "test");

        // Act
        participation.RecalculateTotals();

        // Assert
        participation.Disqualified.ShouldBeTrue();
        participation.Total.ShouldBe(0);
    }

    private static WebApi.Features.Athletes.Athlete CreateAthleteWithBan(
        User creator,
        DateTime banFrom,
        DateTime banTo)
    {
        WebApi.Features.Athletes.Athlete athlete = WebApi.Features.Athletes.Athlete.Create(
            creator, "Jane", "Doe", "f", new Country(), null, null).FromResult();

        WebApi.Features.Athletes.Ban ban = new BanBuilder()
            .WithFromDate(banFrom)
            .WithToDate(banTo)
            .Build();

        athlete.Bans.Add(ban);

        return athlete;
    }
}
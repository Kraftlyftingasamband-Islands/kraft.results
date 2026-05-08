using System.Reflection;

using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.UnitTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.UnitTests.Features.Participations.Participation.RecalculateTotals;

public sealed class SetsDisqualifiedTests
{
    [Fact]
    public void SetsDisqualifiedTrue_WhenAllSquatAttemptsFail()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = CreateParticipationWithNavigations(creator);

        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Squat, round: 2, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Squat, round: 3, weight: 100m, good: false, createdBy: "test");

        // Act
        participation.RecalculateTotals();

        // Assert
        participation.Disqualified.ShouldBeTrue();
    }

    [Fact]
    public void SetsDisqualifiedFalse_WhenAllDisciplinesHaveAtLeastOneGoodLift()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = CreateParticipationWithNavigations(creator);

        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 100m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Bench, round: 1, weight: 60m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Deadlift, round: 1, weight: 120m, good: true, createdBy: "test");

        // Act
        participation.RecalculateTotals();

        // Assert
        participation.Disqualified.ShouldBeFalse();
    }

    [Fact]
    public void SetsDisqualifiedFalse_WhenPreviouslyFailedDisciplineReceivesGoodAttempt()
    {
        // Arrange
        User creator = new UserBuilder().Build();
        WebApi.Features.Participations.Participation participation = CreateParticipationWithNavigations(creator);

        participation.RecordAttempt(Discipline.Squat, round: 1, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Squat, round: 2, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Squat, round: 3, weight: 100m, good: false, createdBy: "test");
        participation.RecordAttempt(Discipline.Bench, round: 1, weight: 60m, good: true, createdBy: "test");
        participation.RecordAttempt(Discipline.Deadlift, round: 1, weight: 120m, good: true, createdBy: "test");
        participation.RecalculateTotals();
        participation.Disqualified.ShouldBeTrue(); // pre-condition

        Attempt firstSquat = participation.Attempts.Single(a => a.Discipline == Discipline.Squat && a.Round == 1);
        participation.UpdateAttempt(firstSquat, weight: 100m, good: true, modifiedBy: "test");

        // Act
        participation.RecalculateTotals();

        // Assert
        participation.Disqualified.ShouldBeFalse();
    }

    internal static WebApi.Features.Participations.Participation CreateParticipationWithNavigations(
        User creator,
        DateTime meetStartDate = default,
        WebApi.Features.Athletes.Athlete? athlete = null)
    {
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        if (athlete is null)
        {
            athlete = WebApi.Features.Athletes.Athlete.Create(
                creator, "John", "Doe", "m", new Country(), null, null).FromResult();
        }

        DateTime resolvedStartDate = meetStartDate == default
            ? new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
            : meetStartDate;

        WebApi.Features.Meets.Meet meet = WebApi.Features.Meets.Meet.Create(
            creator,
            WebApi.Features.Meets.MeetCategory.Powerlifting,
            "Test Meet",
            DateOnly.FromDateTime(resolvedStartDate)).FromResult();

        SetProperty(participation, nameof(WebApi.Features.Participations.Participation.Athlete), athlete);
        SetProperty(participation, nameof(WebApi.Features.Participations.Participation.Meet), meet);

        return participation;
    }

    private static void SetProperty<T>(object target, string propertyName, T value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(target, value);
    }
}
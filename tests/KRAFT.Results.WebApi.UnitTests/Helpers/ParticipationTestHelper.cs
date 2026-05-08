using System.Reflection;

using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.UnitTests.Helpers;

internal static class ParticipationTestHelper
{
    internal static WebApi.Features.Participations.Participation CreateParticipationWithNavigations(
        User creator,
        DateTime? meetStartDate = null,
        WebApi.Features.Athletes.Athlete? athlete = null)
    {
        WebApi.Features.Participations.Participation participation = WebApi.Features.Participations.Participation.Create(
            creator, athleteId: 1, meetId: 1, weightCategoryId: 1, ageCategoryId: 1, bodyWeight: 83.5m).FromResult();

        if (athlete is null)
        {
            athlete = WebApi.Features.Athletes.Athlete.Create(
                creator, "John", "Doe", "m", new Country(), null, null).FromResult();
        }

        DateTime resolvedStartDate = meetStartDate ?? new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);

        WebApi.Features.Meets.Meet meet = WebApi.Features.Meets.Meet.Create(
            creator,
            MeetCategory.Powerlifting,
            "Test Meet",
            DateOnly.FromDateTime(resolvedStartDate)).FromResult();

        SetProperty(participation, nameof(WebApi.Features.Participations.Participation.Athlete), athlete);
        SetProperty(participation, nameof(WebApi.Features.Participations.Participation.Meet), meet);

        return participation;
    }

    internal static void SetProperty<T>(object target, string propertyName, T value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        property.SetValue(target, value);
    }
}
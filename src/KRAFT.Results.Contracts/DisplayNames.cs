using System.Diagnostics.CodeAnalysis;

namespace KRAFT.Results.Contracts;

public static class DisplayNames
{
    public static string ToDisplayName(this Discipline discipline) => discipline switch
    {
        Discipline.Squat => Constants.Squat,
        Discipline.Bench => Constants.Bench,
        Discipline.Deadlift => Constants.Deadlift,
        _ => string.Empty,
    };

    public static string ToAbbreviation(this Discipline discipline) => discipline switch
    {
        Discipline.Squat => "H",
        Discipline.Bench => "B",
        Discipline.Deadlift => "R",
        _ => string.Empty,
    };

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Gender codes are lowercase by convention")]
    public static string ToGenderGroupLabel(this string gender)
    {
        ArgumentNullException.ThrowIfNull(gender);

        return gender.ToLowerInvariant() switch
        {
            "m" => "Karlar",
            "f" => "Konur",
            _ => string.Empty,
        };
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Gender codes are lowercase by convention")]
    public static string ToGenderSingularLabel(this string gender)
    {
        ArgumentNullException.ThrowIfNull(gender);

        return gender.ToLowerInvariant() switch
        {
            "m" => "Karl",
            "f" => "Kona",
            _ => string.Empty,
        };
    }

    public static string EquipmentType(bool isClassic) => isClassic
        ? "Án búnaðar"
        : "Með búnaði";
}
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

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Age category slugs are lowercase by convention")]
    public static string ToAgeCategoryLabel(this string slug, string? gender = null)
    {
        ArgumentNullException.ThrowIfNull(slug);

        string normalized = slug.ToLowerInvariant();

        if (normalized.StartsWith("masters", StringComparison.Ordinal) && normalized.Length == 8)
        {
            char suffix = normalized[7];
            if (suffix >= '1' && suffix <= '4')
            {
                return $"Öldungaflokkur {suffix}";
            }
        }

        return normalized switch
        {
            "open" => "Opinn flokkur",
            "subjunior" => gender?.ToLowerInvariant() == "m" ? "Drengjaflokkur" : "Stúlknaflokkur",
            "junior" => "Unglingaflokkur",
            _ => string.Empty,
        };
    }

    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Age category slugs are lowercase by convention")]
    public static string ToAgeCategoryAgeRange(this string slug)
    {
        ArgumentNullException.ThrowIfNull(slug);

        string normalized = slug.ToLowerInvariant();

        return normalized switch
        {
            "subjunior" => "14-18 ára",
            "junior" => "19-23 ára",
            "masters1" => "40+",
            "masters2" => "50+",
            "masters3" => "60+",
            "masters4" => "70+",
            _ => string.Empty,
        };
    }

    public static string EquipmentType(bool isClassic) => isClassic
        ? "Án búnaðar"
        : "Með búnaði";
}
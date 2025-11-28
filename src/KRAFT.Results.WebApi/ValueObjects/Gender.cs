using System.Diagnostics.CodeAnalysis;

using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed class Gender : ValueObject<string>
{
    internal static readonly Gender Male = new(MaleString);
    internal static readonly Gender Female = new(FemaleString);

    private const string MaleString = "m";
    private const string FemaleString = "f";

    private Gender(string value)
        : base(value)
    {
    }

    internal static bool TryParse(string value, [NotNullWhen(true)] out Gender? gender)
    {
        gender = null;
        value = value.ToLowerInvariant().Trim();

        if (value != MaleString && value != FemaleString)
        {
            return false;
        }

        gender = Parse(value);

        return true;
    }

    internal static Gender Parse(string value) =>
        value == MaleString
            ? Male
            : Female;
}
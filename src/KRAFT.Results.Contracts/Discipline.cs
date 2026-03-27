using System.Diagnostics.CodeAnalysis;

namespace KRAFT.Results.Contracts;

[SuppressMessage("Design", "CA1028:Enum storage should be Int32", Justification = "Matches database tinyint column type")]
public enum Discipline : byte
{
    None = 0,
    Squat = 1,
    Bench = 2,
    Deadlift = 3,
}
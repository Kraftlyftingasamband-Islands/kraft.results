namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetRecordEntry(
    string AthleteName,
    string AthleteSlug,
    string Discipline,
    string WeightCategory,
    string AgeCategory,
    decimal Weight,
    bool IsClassic);
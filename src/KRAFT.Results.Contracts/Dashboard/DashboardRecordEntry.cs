namespace KRAFT.Results.Contracts.Dashboard;

public sealed record DashboardRecordEntry(
    string Lift,
    string AthleteSlug,
    string AthleteName,
    string WeightCategory,
    string AgeCategory,
    bool IsClassic,
    decimal Weight,
    string MeetSlug,
    DateOnly MeetDate);
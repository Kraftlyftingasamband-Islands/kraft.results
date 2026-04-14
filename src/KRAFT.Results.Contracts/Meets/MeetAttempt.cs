using KRAFT.Results.Contracts;

namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetAttempt(
    Discipline Discipline,
    short Round,
    decimal Weight,
    bool IsGood,
    bool IsRecord,
    string? RecordAgeCategory = null);
namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetAttempt(string Discipline, short Round, decimal Weight, bool IsGood);
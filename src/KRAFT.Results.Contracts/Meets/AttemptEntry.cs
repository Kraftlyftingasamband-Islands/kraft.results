namespace KRAFT.Results.Contracts.Meets;

public sealed record class AttemptEntry(Discipline Discipline, short Round, decimal Weight, bool Good);
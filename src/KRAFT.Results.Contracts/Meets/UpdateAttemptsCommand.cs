using System.Collections.ObjectModel;

namespace KRAFT.Results.Contracts.Meets;

public sealed record class UpdateAttemptsCommand(Collection<AttemptEntry> Attempts);
using System.Collections.ObjectModel;

using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class UpdateAttemptsCommandBuilder
{
    private readonly Collection<AttemptEntry> _attempts =
    [
        new("Squat", 1, 100.0m, true),
        new("Squat", 2, 110.0m, true),
        new("Squat", 3, 120.0m, false),
        new("Bench", 1, 60.0m, true),
        new("Bench", 2, 65.0m, true),
        new("Bench", 3, 70.0m, true),
        new("Deadlift", 1, 140.0m, true),
        new("Deadlift", 2, 150.0m, false),
        new("Deadlift", 3, 155.0m, true),
    ];

    public UpdateAttemptsCommandBuilder WithAttempts(Collection<AttemptEntry> attempts)
    {
        _attempts.Clear();
        foreach (AttemptEntry attempt in attempts)
        {
            _attempts.Add(attempt);
        }

        return this;
    }

    public UpdateAttemptsCommand Build() => new(_attempts);
}
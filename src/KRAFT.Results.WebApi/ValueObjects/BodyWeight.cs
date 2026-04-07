using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed class BodyWeight : ValueObject<decimal>
{
    private const decimal MaxValue = 400m;

    private BodyWeight(decimal value)
        : base(value)
    {
    }

    internal static Result<BodyWeight> Create(decimal value)
    {
        if (value <= 0)
        {
            return new Error("BodyWeight.MustBePositive", "Body weight must be greater than zero.");
        }

        if (value > MaxValue)
        {
            return new Error("BodyWeight.TooHigh", "Body weight must not exceed 400 kg.");
        }

        return new BodyWeight(value);
    }
}
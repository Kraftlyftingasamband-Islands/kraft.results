namespace KRAFT.Results.WebApi.Abstractions;

internal sealed record class Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static implicit operator Result(Error error) => Result.Failure(error);
}
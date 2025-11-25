namespace KRAFT.Results.WebApi.Abstractions;

internal class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if ((isSuccess && error != Error.None) ||
            (!isSuccess && error == Error.None))
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public TResult Match<TResult>(Func<TResult> success, Func<Error, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? success() : failure(Error);
    }
}

#pragma warning disable SA1402 // File may only contain a single type
internal sealed class Result<TValue> : Result
#pragma warning restore SA1402 // File may only contain a single type
{
    private readonly TValue? _value;

    public Result(TValue value)
        : base(true, Error.None)
    {
        _value = value;
    }

    public Result(Error error)
        : base(false, error)
    {
    }

    public static implicit operator Result<TValue>(Error error) => new(error);

    public static implicit operator Result<TValue>(TValue value) => new(value);

    public static implicit operator TValue(Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.FromResult();
    }

    public TValue FromResult()
    {
        ArgumentNullException.ThrowIfNull(_value);

        return _value;
    }

    public TResult Match<TResult>(Func<TValue, TResult> success, Func<Error, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(_value);
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? success(_value) : failure(Error);
    }
}
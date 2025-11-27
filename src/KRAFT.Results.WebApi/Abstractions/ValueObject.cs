namespace KRAFT.Results.WebApi.Abstractions;

internal abstract class ValueObject<T>
{
    protected ValueObject(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public static implicit operator T(ValueObject<T> title) => title.Value;

    public override string ToString() => Value?.ToString() ?? string.Empty;
}
namespace KRAFT.Results.WebApi.Abstractions;

internal abstract class ValueObject<T> : IEquatable<ValueObject<T>>
{
    protected ValueObject(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public static implicit operator T(ValueObject<T> title) => title.Value;

    public static bool operator ==(ValueObject<T>? left, ValueObject<T>? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    public static bool operator !=(ValueObject<T>? left, ValueObject<T>? right) => !(left == right);

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ValueObject<T>)obj);
    }

    public bool Equals(ValueObject<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other.GetType() != GetType())
        {
            return false;
        }

        return EqualityComparer<T>.Default.Equals(Value, other.Value);
    }

    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value!);

    public override string ToString() => Value?.ToString() ?? string.Empty;
}
using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed class Email : ValueObject<string>
{
    private Email(string value)
        : base(value.Trim())
    {
    }

    internal static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Error("Email.Empty", "Email is empty");
        }

        if (!value.Contains('@', StringComparison.OrdinalIgnoreCase))
        {
            return new Error("Email.NoAtSymbol", "Email must contain '@'");
        }

        if (value[0] == '@' || value[^1] == '@')
        {
            return new Error("Email.Invalid", "Cannot start or end with '@'");
        }

        return new Email(value);
    }
}
using System.Net.Mail;

namespace PGLN.Auth.Domain.Users;

public sealed record Email
{
    private Email(string value, string normalizedValue)
    {
        Value = value;
        NormalizedValue = normalizedValue;
    }

    public string Value { get; }

    public string NormalizedValue { get; }

    public static Email Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var trimmed = value.Trim();

        if (!MailAddress.TryCreate(trimmed, out var parsedAddress))
        {
            throw new ArgumentException(
                "The email address is invalid.",
                nameof(value));
        }

        var normalized = parsedAddress.Address.ToUpperInvariant();

        return new Email(
            parsedAddress.Address,
            normalized);
    }

    public override string ToString()
    {
        return Value;
    }
}
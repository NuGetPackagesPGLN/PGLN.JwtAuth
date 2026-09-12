namespace PGLN.Auth.Domain.EmailChangeTokens;

public readonly record struct EmailChangeTokenId(
    Guid Value)
{
    public static EmailChangeTokenId New()
    {
        return new EmailChangeTokenId(
            Guid.NewGuid());
    }

    public static EmailChangeTokenId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Email change token ID cannot be empty.",
                nameof(value));
        }

        return new EmailChangeTokenId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

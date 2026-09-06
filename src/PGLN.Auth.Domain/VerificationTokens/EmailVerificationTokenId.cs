namespace PGLN.Auth.Domain.VerificationTokens;

public readonly record struct EmailVerificationTokenId(Guid Value)
{
    public static EmailVerificationTokenId New()
    {
        return new EmailVerificationTokenId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

namespace PGLN.Auth.Domain.PasswordResets;

public readonly record struct PasswordResetTokenId(
    Guid Value)
{
    public static PasswordResetTokenId New()
    {
        return new PasswordResetTokenId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

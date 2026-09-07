namespace PGLN.Auth.Domain.RefreshTokens;

public readonly record struct RefreshTokenId(Guid Value)
{
    public static RefreshTokenId New()
    {
        return new RefreshTokenId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

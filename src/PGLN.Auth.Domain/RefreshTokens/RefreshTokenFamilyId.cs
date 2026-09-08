namespace PGLN.Auth.Domain.RefreshTokens;

public readonly record struct RefreshTokenFamilyId(
    Guid Value)
{
    public static RefreshTokenFamilyId New()
    {
        return new RefreshTokenFamilyId(
            Guid.NewGuid());
    }

    public static RefreshTokenFamilyId From(
        Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "Refresh token family id cannot be empty.",
                nameof(value));
        }

        return new RefreshTokenFamilyId(
            value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

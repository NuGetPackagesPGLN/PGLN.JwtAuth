namespace PGLN.Auth.Domain.Sessions;

public readonly record struct AuthSessionId(Guid Value)
{
    public static AuthSessionId New()
    {
        return new AuthSessionId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

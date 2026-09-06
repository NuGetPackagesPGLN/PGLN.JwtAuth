namespace PGLN.Auth.Domain.Users;

public readonly record struct UserId(Guid Value)
{
    public static UserId New()
    {
        return new UserId(Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
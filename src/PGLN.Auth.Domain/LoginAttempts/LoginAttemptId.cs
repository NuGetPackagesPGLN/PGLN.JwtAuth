namespace PGLN.Auth.Domain.LoginAttempts;

public readonly record struct LoginAttemptId(Guid Value)
{
    public static LoginAttemptId New()
    {
        return new LoginAttemptId(
            Guid.NewGuid());
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

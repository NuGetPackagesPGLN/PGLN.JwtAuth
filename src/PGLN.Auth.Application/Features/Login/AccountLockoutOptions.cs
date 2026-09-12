namespace PGLN.Auth.Application.Features.Login;

public sealed class AccountLockoutOptions
{
    public int MaxFailedAttempts { get; init; } =
        5;

    public TimeSpan FailureWindow { get; init; } =
        TimeSpan.FromMinutes(15);

    public TimeSpan LockoutDuration { get; init; } =
        TimeSpan.FromMinutes(15);

    public void Validate()
    {
        if (MaxFailedAttempts <= 0)
        {
            throw new InvalidOperationException(
                "Maximum failed login attempts must be greater than zero.");
        }

        if (FailureWindow <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Login failure window must be greater than zero.");
        }

        if (LockoutDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Account lockout duration must be greater than zero.");
        }
    }
}

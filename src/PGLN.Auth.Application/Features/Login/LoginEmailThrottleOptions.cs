namespace PGLN.Auth.Application.Features.Login;

public sealed class LoginEmailThrottleOptions
{
    public int MaxFailedAttempts { get; init; } =
        10;

    public TimeSpan Window { get; init; } =
        TimeSpan.FromMinutes(5);

    public void Validate()
    {
        if (MaxFailedAttempts <= 0)
        {
            throw new InvalidOperationException(
                "Login email throttle maximum failed attempts must be greater than zero.");
        }

        if (Window <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Login email throttle window must be greater than zero.");
        }
    }
}

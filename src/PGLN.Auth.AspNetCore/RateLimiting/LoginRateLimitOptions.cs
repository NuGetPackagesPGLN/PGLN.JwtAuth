namespace PGLN.Auth.AspNetCore.RateLimiting;

public sealed class LoginRateLimitOptions
{
    public const string PolicyName =
        "PGLNAuth.Login";

    public int PermitLimit { get; init; } =
        10;

    public TimeSpan Window { get; init; } =
        TimeSpan.FromMinutes(1);

    public int QueueLimit { get; init; } =
        0;

    public void Validate()
    {
        if (PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                "Login rate-limit permit limit must be greater than zero.");
        }

        if (Window <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Login rate-limit window must be greater than zero.");
        }

        if (QueueLimit < 0)
        {
            throw new InvalidOperationException(
                "Login rate-limit queue limit cannot be negative.");
        }
    }
}

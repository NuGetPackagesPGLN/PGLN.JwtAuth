namespace PGLN.Auth.AspNetCore.RateLimiting;

public sealed class StepUpRateLimitOptions
{
    public const string PolicyName =
        "PGLNAuth.StepUp";

    public int PermitLimit { get; init; } =
        5;

    public TimeSpan Window { get; init; } =
        TimeSpan.FromMinutes(1);

    public int QueueLimit { get; init; } =
        0;

    public void Validate()
    {
        if (PermitLimit <= 0)
        {
            throw new InvalidOperationException(
                "Step-up rate-limit permit limit must be greater than zero.");
        }

        if (Window <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Step-up rate-limit window must be greater than zero.");
        }

        if (QueueLimit < 0)
        {
            throw new InvalidOperationException(
                "Step-up rate-limit queue limit cannot be negative.");
        }
    }
}

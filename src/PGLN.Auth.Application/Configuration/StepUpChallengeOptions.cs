namespace PGLN.Auth.Application.Configuration;

public sealed class StepUpChallengeOptions
{
    public const string SectionName =
        "PGLNAuth:StepUp";

    public TimeSpan Lifetime { get; init; } =
        TimeSpan.FromMinutes(10);

    public int MaxFailedAttempts { get; init; } =
        5;

    public void Validate()
    {
        if (Lifetime <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Step-up challenge lifetime must be greater than zero.");
        }

        if (MaxFailedAttempts <= 0)
        {
            throw new InvalidOperationException(
                "Step-up maximum failed attempts must be greater than zero.");
        }
    }
}

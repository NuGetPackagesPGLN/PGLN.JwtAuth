namespace PGLN.Auth.Application.Configuration;

public sealed class StepUpChallengeOptions
{
    public const string SectionName =
        "Authentication:StepUp";

    public TimeSpan Lifetime { get; init; } =
        TimeSpan.FromMinutes(10);

    public int MaxFailedAttempts { get; init; } =
        5;
}

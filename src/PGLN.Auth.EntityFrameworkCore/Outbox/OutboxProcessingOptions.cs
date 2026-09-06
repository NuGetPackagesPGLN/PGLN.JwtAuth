namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public sealed class OutboxProcessingOptions
{
    public int BatchSize { get; init; } = 20;

    public int MaximumAttempts { get; init; } = 5;

    public TimeSpan InitialRetryDelay { get; init; } =
        TimeSpan.FromSeconds(10);

    public TimeSpan MaximumRetryDelay { get; init; } =
        TimeSpan.FromMinutes(15);

    public TimeSpan ClaimDuration { get; init; } =
        TimeSpan.FromMinutes(2);

    public void Validate()
    {
        if (BatchSize <= 0)
        {
            throw new InvalidOperationException(
                "Outbox batch size must be greater than zero.");
        }

        if (MaximumAttempts <= 0)
        {
            throw new InvalidOperationException(
                "Outbox maximum attempts must be greater than zero.");
        }

        if (InitialRetryDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Outbox initial retry delay must be greater than zero.");
        }

        if (MaximumRetryDelay < InitialRetryDelay)
        {
            throw new InvalidOperationException(
                "Outbox maximum retry delay cannot be less than the initial retry delay.");
        }

        if (ClaimDuration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Outbox claim duration must be greater than zero.");
        }
    }
}

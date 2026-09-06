namespace PGLN.Auth.AspNetCore.Outbox;

public sealed class OutboxBackgroundWorkerOptions
{
    public bool Enabled { get; init; } = true;

    public TimeSpan PollInterval { get; init; } =
        TimeSpan.FromSeconds(5);

    public string WorkerIdPrefix { get; init; } =
        "aspnet";

    public void Validate()
    {
        if (PollInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Outbox polling interval must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(
            WorkerIdPrefix))
        {
            throw new InvalidOperationException(
                "Outbox worker ID prefix is required.");
        }
    }
}

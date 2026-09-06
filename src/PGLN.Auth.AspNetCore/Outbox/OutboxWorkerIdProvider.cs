namespace PGLN.Auth.AspNetCore.Outbox;

public sealed class OutboxWorkerIdProvider
    : IOutboxWorkerIdProvider
{
    public OutboxWorkerIdProvider(
        OutboxBackgroundWorkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();

        WorkerId =
            $"{options.WorkerIdPrefix}-{Environment.MachineName}-{Guid.NewGuid():N}";
    }

    public string WorkerId { get; }
}

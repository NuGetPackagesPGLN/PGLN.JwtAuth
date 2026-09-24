namespace PGLN.Auth.AspNetCore.Outbox;

internal interface IOutboxWorkerIdProvider
{
    string WorkerId { get; }
}

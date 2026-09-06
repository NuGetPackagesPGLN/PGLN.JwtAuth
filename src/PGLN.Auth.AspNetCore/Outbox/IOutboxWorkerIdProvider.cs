namespace PGLN.Auth.AspNetCore.Outbox;

public interface IOutboxWorkerIdProvider
{
    string WorkerId { get; }
}

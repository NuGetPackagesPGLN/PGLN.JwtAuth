namespace PGLN.Auth.Application.Abstractions.Outbox;

public interface IOutboxProcessor
{
    Task<int> ProcessAsync(
        string workerId,
        CancellationToken cancellationToken = default);
}

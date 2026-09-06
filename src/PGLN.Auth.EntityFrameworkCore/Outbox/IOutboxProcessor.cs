namespace PGLN.Auth.EntityFrameworkCore.Outbox;

public interface IOutboxProcessor
{
    Task<int> ProcessAsync(
        string workerId,
        CancellationToken cancellationToken = default);
}

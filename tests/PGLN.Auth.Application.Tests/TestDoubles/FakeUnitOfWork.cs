using PGLN.Auth.Application.Abstractions.Persistence;

namespace PGLN.Auth.Application.Tests.TestDoubles;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SaveChangesCallCount++;

        return Task.FromResult(1);
    }
}

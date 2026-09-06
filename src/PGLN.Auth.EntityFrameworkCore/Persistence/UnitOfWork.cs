using PGLN.Auth.Application.Abstractions.Persistence;

namespace PGLN.Auth.EntityFrameworkCore.Persistence;

public sealed class UnitOfWork
    : IUnitOfWork
{
    private readonly AuthDbContext _dbContext;

    public UnitOfWork(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}

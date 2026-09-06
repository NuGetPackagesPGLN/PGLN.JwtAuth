using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class UserRepository
    : IUserRepository
{
    private readonly AuthDbContext _dbContext;

    public UserRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<User?> GetByIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .SingleOrDefaultAsync(
                user => user.Id == userId,
                cancellationToken);
    }

    public Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedEmail);

        return _dbContext.Users
            .SingleOrDefaultAsync(
                user =>
                    user.NormalizedEmail ==
                    normalizedEmail,
                cancellationToken);
    }

    public Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedEmail);

        return _dbContext.Users
            .AnyAsync(
                user =>
                    user.NormalizedEmail ==
                    normalizedEmail,
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _dbContext.Users.AddAsync(
            user,
            cancellationToken);
    }
}

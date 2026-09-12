using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.LoginAttempts;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class LoginAttemptRepository
    : ILoginAttemptRepository
{
    private readonly AuthDbContext _dbContext;

    public LoginAttemptRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        _dbContext =
            dbContext;
    }

    public async Task AddAsync(
        LoginAttempt loginAttempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            loginAttempt);

        await _dbContext
            .LoginAttempts
            .AddAsync(
                loginAttempt,
                cancellationToken);
    }

    public Task<int> CountFailedAttemptsAsync(
        string normalizedEmail,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedEmail);

        return _dbContext.LoginAttempts
            .AsNoTracking()
            .CountAsync(
                attempt =>
                    attempt.Email == normalizedEmail &&
                    !attempt.Succeeded &&
                    attempt.AttemptedAtUtc >= sinceUtc,
                cancellationToken);
    }
}



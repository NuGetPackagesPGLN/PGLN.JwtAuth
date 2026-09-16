using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class AuthSessionRepository
    : IAuthSessionRepository
{
    private readonly AuthDbContext _dbContext;

    public AuthSessionRepository(
        AuthDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<AuthSession?> GetByIdAsync(
        AuthSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AuthSessions
            .SingleOrDefaultAsync(
                session =>
                    session.Id ==
                    sessionId,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<AuthSession>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuthSessions
            .Where(
                session =>
                    session.UserId ==
                    userId)
            .OrderByDescending(
                session =>
                    session.LastSeenAtUtc)
            .ToListAsync(
                cancellationToken);
    }

    public Task<AuthSession?> GetActiveByDeviceIdHashAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceIdHash);

        return _dbContext.AuthSessions
            .SingleOrDefaultAsync(
                session =>
                    session.UserId ==
                    userId &&
                    session.DeviceIdHash ==
                    deviceIdHash &&
                    session.RevokedAtUtc ==
                    null,
                cancellationToken);
    }

    public Task<bool> HasSeenDeviceAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            deviceIdHash);

        return _dbContext.AuthSessions
            .AnyAsync(
                session =>
                    session.UserId ==
                    userId &&
                    session.DeviceIdHash ==
                    deviceIdHash,
                cancellationToken);
    }
    public Task<bool> HasAnySessionAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AuthSessions
            .AnyAsync(
                session =>
                    session.UserId ==
                    userId,
                cancellationToken);
    }

    public Task AddAsync(
        AuthSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            session);

        return _dbContext.AuthSessions
            .AddAsync(
                session,
                cancellationToken)
            .AsTask();
    }
}

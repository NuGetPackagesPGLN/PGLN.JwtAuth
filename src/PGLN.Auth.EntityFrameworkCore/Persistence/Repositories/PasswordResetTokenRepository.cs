using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class PasswordResetTokenRepository
    : IPasswordResetTokenRepository
{
    private readonly AuthDbContext _dbContext;

    public PasswordResetTokenRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        _dbContext =
            dbContext;
    }

    public async Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return await _dbContext
            .PasswordResetTokens
            .SingleOrDefaultAsync(
                token =>
                    token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .PasswordResetTokens
            .Where(
                token =>
                    token.UserId == userId &&
                    token.UsedAtUtc == null &&
                    token.ExpiresAtUtc > now)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            token);

        await _dbContext
            .PasswordResetTokens
            .AddAsync(
                token,
                cancellationToken);
    }
}

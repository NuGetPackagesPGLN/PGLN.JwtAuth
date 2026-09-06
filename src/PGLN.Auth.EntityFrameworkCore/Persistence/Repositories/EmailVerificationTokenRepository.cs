using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class EmailVerificationTokenRepository
    : IEmailVerificationTokenRepository
{
    private readonly AuthDbContext _dbContext;

    public EmailVerificationTokenRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return _dbContext
            .EmailVerificationTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmailVerificationToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var tokens =
            await _dbContext
                .EmailVerificationTokens
                .Where(
                    token =>
                        token.UserId == userId &&
                        token.UsedAtUtc == null)
                .ToArrayAsync(
                    cancellationToken);

        return tokens;
    }

    public async Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        await _dbContext
            .EmailVerificationTokens
            .AddAsync(
                token,
                cancellationToken);
    }
}

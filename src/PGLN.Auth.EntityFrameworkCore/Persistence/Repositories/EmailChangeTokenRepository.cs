using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class EmailChangeTokenRepository
    : IEmailChangeTokenRepository
{
    private readonly AuthDbContext _dbContext;

    public EmailChangeTokenRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public Task<EmailChangeToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return _dbContext
            .EmailChangeTokens
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmailChangeToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var tokens =
            await _dbContext
                .EmailChangeTokens
                .Where(
                    token =>
                        token.UserId == userId &&
                        token.UsedAtUtc == null)
                .ToArrayAsync(
                    cancellationToken);

        return tokens;
    }

    public async Task AddAsync(
        EmailChangeToken token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(token);

        await _dbContext
            .EmailChangeTokens
            .AddAsync(
                token,
                cancellationToken);
    }
}

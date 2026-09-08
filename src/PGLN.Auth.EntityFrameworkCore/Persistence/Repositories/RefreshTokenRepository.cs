using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.RefreshTokens;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class RefreshTokenRepository
    : IRefreshTokenRepository
{
    private readonly AuthDbContext _dbContext;

    public RefreshTokenRepository(
        AuthDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        _dbContext =
            dbContext;
    }

    public Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            tokenHash);

        return _dbContext
            .RefreshTokens
            .SingleOrDefaultAsync(
                token =>
                    token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            refreshToken);

        await _dbContext
            .RefreshTokens
            .AddAsync(
                refreshToken,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<RefreshToken>> GetByFamilyIdAsync(
        RefreshTokenFamilyId familyId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext
            .RefreshTokens
            .Where(
                refreshToken =>
                    refreshToken.FamilyId == familyId)
            .ToListAsync(
                cancellationToken);
    }
}



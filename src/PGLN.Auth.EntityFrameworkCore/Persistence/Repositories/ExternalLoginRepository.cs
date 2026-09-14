using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class ExternalLoginRepository
    : IExternalLoginRepository
{
    private readonly AuthDbContext _dbContext;

    public ExternalLoginRepository(
        AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ExternalLogin?> GetByIdAsync(
        ExternalLoginId id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExternalLogins
            .SingleOrDefaultAsync(
                externalLogin =>
                    externalLogin.Id == id,
                cancellationToken);
    }

    public Task<ExternalLogin?> GetByProviderAndSubjectAsync(
        ExternalLoginProvider provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ExternalLogins
            .SingleOrDefaultAsync(
                externalLogin =>
                    externalLogin.Provider == provider &&
                    externalLogin.ProviderSubject ==
                    providerSubject,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExternalLogin>>
        GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.ExternalLogins
            .Where(
                externalLogin =>
                    externalLogin.UserId == userId)
            .OrderBy(
                externalLogin =>
                    externalLogin.Provider)
            .ToListAsync(
                cancellationToken);
    }

    public async Task AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.ExternalLogins
            .AddAsync(
                externalLogin,
                cancellationToken);
    }
}


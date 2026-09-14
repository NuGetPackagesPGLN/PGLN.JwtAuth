using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Domain.ExternalLogins;

public interface IExternalLoginRepository
{
    Task<ExternalLogin?> GetByIdAsync(
        ExternalLoginId id,
        CancellationToken cancellationToken = default);

    Task<ExternalLogin?> GetByProviderAndSubjectAsync(
        ExternalLoginProvider provider,
        string providerSubject,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ExternalLogin>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ExternalLogin externalLogin,
        CancellationToken cancellationToken = default);
}

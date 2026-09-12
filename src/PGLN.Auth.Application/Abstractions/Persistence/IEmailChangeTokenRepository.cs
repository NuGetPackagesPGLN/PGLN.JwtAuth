using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface IEmailChangeTokenRepository
{
    Task<EmailChangeToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EmailChangeToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        EmailChangeToken token,
        CancellationToken cancellationToken = default);
}

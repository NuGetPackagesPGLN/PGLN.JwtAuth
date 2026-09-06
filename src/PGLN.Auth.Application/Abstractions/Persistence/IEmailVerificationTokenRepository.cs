using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface IEmailVerificationTokenRepository
{
    Task<EmailVerificationToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<EmailVerificationToken>> GetActiveByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        EmailVerificationToken token,
        CancellationToken cancellationToken = default);
}

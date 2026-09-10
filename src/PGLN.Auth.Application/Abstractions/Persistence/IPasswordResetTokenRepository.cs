using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<PasswordResetToken>> GetActiveByUserIdAsync(
        UserId userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);
}

using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RefreshToken>> GetByFamilyIdAsync(
        RefreshTokenFamilyId familyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RefreshToken>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<RefreshToken>> GetBySessionIdAsync(
        AuthSessionId sessionId,
        CancellationToken cancellationToken = default);
}

using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface IAuthSessionRepository
{
    Task<AuthSession?> GetByIdAsync(
        AuthSessionId sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AuthSession>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<AuthSession?> GetActiveByDeviceIdHashAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default);

    Task<bool> HasSeenDeviceAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnySessionAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AuthSession session,
        CancellationToken cancellationToken = default);
}

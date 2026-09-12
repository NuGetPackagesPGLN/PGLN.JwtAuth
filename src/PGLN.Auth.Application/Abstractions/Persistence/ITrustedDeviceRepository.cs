using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Abstractions.Persistence;

public interface ITrustedDeviceRepository
{
    Task<TrustedDevice?> GetByIdAsync(
        TrustedDeviceId trustedDeviceId,
        CancellationToken cancellationToken = default);

    Task<TrustedDevice?> GetByUserAndDeviceHashAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TrustedDevice>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TrustedDevice trustedDevice,
        CancellationToken cancellationToken = default);
}

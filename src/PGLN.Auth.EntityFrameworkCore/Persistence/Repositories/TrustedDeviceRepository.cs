using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

public sealed class TrustedDeviceRepository
    : ITrustedDeviceRepository
{
    private readonly AuthDbContext _dbContext;

    public TrustedDeviceRepository(
        AuthDbContext dbContext)
    {
        _dbContext =
            dbContext;
    }

    public Task<TrustedDevice?> GetByIdAsync(
        TrustedDeviceId trustedDeviceId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TrustedDevices
            .SingleOrDefaultAsync(
                device =>
                    device.Id == trustedDeviceId,
                cancellationToken);
    }

    public Task<TrustedDevice?> GetByUserAndDeviceHashAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TrustedDevices
            .SingleOrDefaultAsync(
                device =>
                    device.UserId == userId &&
                    device.DeviceIdHash == deviceIdHash,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<TrustedDevice>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var devices =
            await _dbContext.TrustedDevices
                .Where(
                    device =>
                        device.UserId == userId)
                .ToArrayAsync(
                    cancellationToken);

        return devices
            .OrderByDescending(
                device =>
                    device.TrustedAtUtc)
            .ToArray();
    }

    public async Task AddAsync(
        TrustedDevice trustedDevice,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.TrustedDevices.AddAsync(
            trustedDevice,
            cancellationToken);
    }
}


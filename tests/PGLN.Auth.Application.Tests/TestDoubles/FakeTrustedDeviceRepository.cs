using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.TrustedDevices;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeTrustedDeviceRepository
    : ITrustedDeviceRepository
{
    private readonly List<TrustedDevice> _devices = [];

    public IReadOnlyCollection<TrustedDevice> Devices =>
        _devices.AsReadOnly();

    public Task<TrustedDevice?> GetByIdAsync(
        TrustedDeviceId trustedDeviceId,
        CancellationToken cancellationToken = default)
    {
        var device =
            _devices.SingleOrDefault(
                item =>
                    item.Id ==
                    trustedDeviceId);

        return Task.FromResult(
            device);
    }

    public Task<TrustedDevice?> GetByUserAndDeviceHashAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        var device =
            _devices.SingleOrDefault(
                item =>
                    item.UserId == userId &&
                    item.DeviceIdHash == deviceIdHash);

        return Task.FromResult(
            device);
    }

    public Task<IReadOnlyCollection<TrustedDevice>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<TrustedDevice> devices =
            _devices
                .Where(
                    item =>
                        item.UserId == userId)
                .ToArray();

        return Task.FromResult(
            devices);
    }

    public Task AddAsync(
        TrustedDevice trustedDevice,
        CancellationToken cancellationToken = default)
    {
        _devices.Add(
            trustedDevice);

        return Task.CompletedTask;
    }

    public void Seed(
        TrustedDevice trustedDevice)
    {
        _devices.Add(
            trustedDevice);
    }
}

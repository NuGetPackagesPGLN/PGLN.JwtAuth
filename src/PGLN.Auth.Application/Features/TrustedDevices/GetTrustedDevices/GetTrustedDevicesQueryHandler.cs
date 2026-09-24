using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;

namespace PGLN.Auth.Application.Features.TrustedDevices.GetTrustedDevices;

internal sealed class GetTrustedDevicesQueryHandler
    : IQueryHandler<
        GetTrustedDevicesQuery,
        IReadOnlyCollection<TrustedDeviceResponse>>
{
    private readonly ITrustedDeviceRepository _trustedDeviceRepository;

    public GetTrustedDevicesQueryHandler(
        ITrustedDeviceRepository trustedDeviceRepository)
    {
        _trustedDeviceRepository =
            trustedDeviceRepository;
    }

    public async Task<IReadOnlyCollection<TrustedDeviceResponse>> HandleAsync(
        GetTrustedDevicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var trustedDevices =
            await _trustedDeviceRepository
                .GetByUserIdAsync(
                    query.UserId,
                    cancellationToken);

        return trustedDevices
            .Select(
                device =>
                    new TrustedDeviceResponse(
                        device.Id,
                        device.DeviceName,
                        device.TrustedAtUtc,
                        device.RevokedAtUtc,
                        device.IsTrusted))
            .ToArray();
    }
}


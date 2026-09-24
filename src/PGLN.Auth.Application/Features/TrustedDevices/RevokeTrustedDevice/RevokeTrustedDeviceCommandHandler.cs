using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.TrustedDevices.RevokeTrustedDevice;

internal sealed class RevokeTrustedDeviceCommandHandler
    : ICommandHandler<RevokeTrustedDeviceCommand, Result>
{
    private static readonly Error TrustedDeviceNotFoundError =
        new(
            "TrustedDevices.NotFound",
            "The requested trusted device was not found.");

    private const string RevocationReason =
        "UserRevokedTrust";

    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeTrustedDeviceCommandHandler(
        ITrustedDeviceRepository trustedDeviceRepository,
        IAuthSessionRepository authSessionRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _trustedDeviceRepository =
            trustedDeviceRepository;

        _authSessionRepository =
            authSessionRepository;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;
    }

    public async Task<Result> HandleAsync(
        RevokeTrustedDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var trustedDevice =
            await _trustedDeviceRepository
                .GetByIdAsync(
                    command.TrustedDeviceId,
                    cancellationToken);

        if (trustedDevice is null ||
            trustedDevice.UserId != command.UserId)
        {
            return Result.Failure(
                TrustedDeviceNotFoundError);
        }

        var now =
            _clock.UtcNow;

        trustedDevice.Revoke(
            now,
            RevocationReason);

        var sessions =
            await _authSessionRepository
                .GetByUserIdAsync(
                    command.UserId,
                    cancellationToken);

        foreach (var session in sessions)
        {
            if (session.DeviceIdHash !=
                trustedDevice.DeviceIdHash)
            {
                continue;
            }

            if (!session.IsActive)
            {
                continue;
            }

            session.RevokeDeviceTrust();
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

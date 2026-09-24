using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.TrustedDevices;

namespace PGLN.Auth.Application.Features.Sessions.TrustDevice;

internal sealed class TrustDeviceCommandHandler
    : ICommandHandler<TrustDeviceCommand, Result>
{
    private static readonly Error SessionNotFoundError =
        new(
            "Sessions.NotFound",
            "The requested session was not found.");

    private static readonly Error SessionRevokedError =
        new(
            "Sessions.Revoked",
            "A revoked session cannot be trusted.");

    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public TrustDeviceCommandHandler(
        IAuthSessionRepository authSessionRepository,
        ITrustedDeviceRepository trustedDeviceRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _authSessionRepository =
            authSessionRepository;

        _trustedDeviceRepository =
            trustedDeviceRepository;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;
    }

    public async Task<Result> HandleAsync(
        TrustDeviceCommand command,
        CancellationToken cancellationToken = default)
    {
        var session =
            await _authSessionRepository
                .GetByIdAsync(
                    command.SessionId,
                    cancellationToken);

        if (session is null ||
            session.UserId != command.UserId)
        {
            return Result.Failure(
                SessionNotFoundError);
        }

        if (session.IsRevoked)
        {
            return Result.Failure(
                SessionRevokedError);
        }

        var trustedDevice =
            await _trustedDeviceRepository
                .GetByUserAndDeviceHashAsync(
                    command.UserId,
                    session.DeviceIdHash,
                    cancellationToken);

        var now =
            _clock.UtcNow;

        if (trustedDevice is null)
        {
            trustedDevice =
                TrustedDevice.Create(
                    TrustedDeviceId.New(),
                    command.UserId,
                    session.DeviceIdHash,
                    session.DeviceName,
                    now);

            await _trustedDeviceRepository
                .AddAsync(
                    trustedDevice,
                    cancellationToken);
        }
        else if (trustedDevice.IsRevoked)
        {
            trustedDevice.TrustAgain(
                now,
                session.DeviceName);
        }

        session.TrustDevice();

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

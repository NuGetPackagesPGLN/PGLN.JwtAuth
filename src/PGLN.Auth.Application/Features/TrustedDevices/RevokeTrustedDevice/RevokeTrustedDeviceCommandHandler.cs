using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.TrustedDevices.RevokeTrustedDevice;

public sealed class RevokeTrustedDeviceCommandHandler
    : ICommandHandler<RevokeTrustedDeviceCommand, Result>
{
    private static readonly Error TrustedDeviceNotFoundError =
        new(
            "TrustedDevices.NotFound",
            "The requested trusted device was not found.");

    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeTrustedDeviceCommandHandler(
        ITrustedDeviceRepository trustedDeviceRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _trustedDeviceRepository =
            trustedDeviceRepository;

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

        trustedDevice.Revoke(
            _clock.UtcNow,
            "UserRevokedTrust");

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

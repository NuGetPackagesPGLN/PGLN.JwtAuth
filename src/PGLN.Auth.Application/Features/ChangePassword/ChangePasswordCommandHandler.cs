using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ChangePassword;

public sealed class ChangePasswordCommandHandler
    : ICommandHandler<
        ChangePasswordCommand,
        Result>
{
    private const string RefreshTokenRevocationReason =
        "PasswordChanged";

    private readonly IUserRepository
        _userRepository;

    private readonly IRefreshTokenRepository
        _refreshTokenRepository;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IIntegrationEventPublisher
        _integrationEventPublisher;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IClock
        _clock;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(
            userRepository);

        ArgumentNullException.ThrowIfNull(
            refreshTokenRepository);

        ArgumentNullException.ThrowIfNull(
            passwordHasher);

        ArgumentNullException.ThrowIfNull(
            integrationEventPublisher);

        ArgumentNullException.ThrowIfNull(
            unitOfWork);

        ArgumentNullException.ThrowIfNull(
            clock);

        _userRepository =
            userRepository;

        _refreshTokenRepository =
            refreshTokenRepository;

        _passwordHasher =
            passwordHasher;

        _integrationEventPublisher =
            integrationEventPublisher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;
    }

    public async Task<Result> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        if (!Guid.TryParse(
                command.UserId,
                out var parsedUserId))
        {
            return Result.Failure(
                ChangePasswordErrors.UserNotFound);
        }

        var userId =
            new UserId(
                parsedUserId);

        var user =
            await _userRepository
                .GetByIdAsync(
                    userId,
                    cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                ChangePasswordErrors.UserNotFound);
        }

        var currentPasswordIsValid =
            user.HasPassword &&
            _passwordHasher.Verify(
                command.CurrentPassword,
                user.PasswordHash!);

        if (!currentPasswordIsValid)
        {
            return Result.Failure(
                ChangePasswordErrors.InvalidCurrentPassword);
        }

        var newPasswordHash =
            _passwordHasher.Hash(
                command.NewPassword);

        var now =
            _clock.UtcNow;

        user.ChangePassword(
            newPasswordHash,
            now);

        var refreshTokens =
            await _refreshTokenRepository
                .GetByUserIdAsync(
                    user.Id,
                    cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            if (!refreshToken.IsActive(
                    now))
            {
                continue;
            }

            refreshToken.Revoke(
                now,
                RefreshTokenRevocationReason);
        }

        var notificationRequested =
            new PasswordChangedNotificationRequested(
                Guid.NewGuid(),
                user.Id,
                user.Email.Value,
                now);

        await _integrationEventPublisher
            .PublishAsync(
                notificationRequested,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}


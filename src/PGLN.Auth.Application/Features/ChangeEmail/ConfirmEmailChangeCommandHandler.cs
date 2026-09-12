using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ChangeEmail;

public sealed class ConfirmEmailChangeCommandHandler
    : ICommandHandler<
        ConfirmEmailChangeCommand,
        Result>
{
    private readonly IUserRepository
        _userRepository;

    private readonly IEmailChangeTokenRepository
        _emailChangeTokenRepository;

    private readonly ITokenHasher
        _tokenHasher;

    private readonly IIntegrationEventPublisher
        _integrationEventPublisher;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IClock
        _clock;

    public ConfirmEmailChangeCommandHandler(
        IUserRepository userRepository,
        IEmailChangeTokenRepository emailChangeTokenRepository,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(emailChangeTokenRepository);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);

        _userRepository = userRepository;
        _emailChangeTokenRepository = emailChangeTokenRepository;
        _tokenHasher = tokenHasher;
        _integrationEventPublisher = integrationEventPublisher;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(
        ConfirmEmailChangeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Token))
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.InvalidToken);
        }

        var tokenHash =
            _tokenHasher.Hash(command.Token);

        var emailChangeToken =
            await _emailChangeTokenRepository
                .GetByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

        if (emailChangeToken is null)
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.InvalidToken);
        }

        var now = _clock.UtcNow;

        if (emailChangeToken.IsUsed)
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.TokenAlreadyUsed);
        }

        if (emailChangeToken.IsExpired(now))
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.ExpiredToken);
        }

        var user =
            await _userRepository
                .GetByIdAsync(
                    emailChangeToken.UserId,
                    cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.UserNotFound);
        }

        Email newEmail;

        try
        {
            newEmail =
                Email.Create(
                    emailChangeToken.NewEmail);
        }
        catch (ArgumentException)
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.InvalidToken);
        }

        var existingUser =
            await _userRepository
                .GetByNormalizedEmailAsync(
                    newEmail.NormalizedValue,
                    cancellationToken);

        if (existingUser is not null &&
            existingUser.Id != user.Id)
        {
            return Result.Failure(
                ConfirmEmailChangeErrors.EmailAlreadyInUse);
        }

        var oldEmail =
            user.Email.Value;

        user.ConfirmEmailChange(
            newEmail,
            now);

        emailChangeToken.MarkAsUsed(
            now);

        var activeTokens =
            await _emailChangeTokenRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            if (activeToken.Id == emailChangeToken.Id)
            {
                continue;
            }

            activeToken.Revoke(now);
        }

        await _integrationEventPublisher
            .PublishAsync(
                new EmailChangedNotificationRequested(
                    Guid.NewGuid(),
                    user.Id,
                    oldEmail,
                    newEmail.Value,
                    now),
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

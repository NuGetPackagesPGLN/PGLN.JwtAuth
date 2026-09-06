using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;

namespace PGLN.Auth.Application.Features.EmailConfirmation;

public sealed class ConfirmEmailCommandHandler
    : ICommandHandler<
        ConfirmEmailCommand,
        Result<ConfirmEmailResult>>
{
    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailCommandHandler(
        IEmailVerificationTokenRepository tokenRepository,
        IUserRepository userRepository,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(tokenRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
        _integrationEventPublisher = integrationEventPublisher;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConfirmEmailResult>> HandleAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tokenHash =
            _tokenHasher.Hash(
                command.Token);

        var token =
            await _tokenRepository.GetByTokenHashAsync(
                tokenHash,
                cancellationToken);

        if (token is null)
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.InvalidToken);
        }

        if (token.IsUsed)
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.TokenAlreadyUsed);
        }

        var now =
            _clock.UtcNow;

        if (token.IsExpired(now))
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.ExpiredToken);
        }

        var user =
            await _userRepository.GetByIdAsync(
                token.UserId,
                cancellationToken);

        if (user is null)
        {
            return Result<ConfirmEmailResult>.Failure(
                EmailConfirmationErrors.UserNotFound);
        }

        user.ConfirmEmail(
            now);

        token.MarkAsUsed(
            now);

        var welcomeRequested =
            new WelcomeEmailRequested(
                Guid.NewGuid(),
                user.Id,
                user.Email.Value,
                now);

        await _integrationEventPublisher.PublishAsync(
            welcomeRequested,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ConfirmEmailResult>.Success(
            new ConfirmEmailResult(
                user.Id,
                user.Email.Value,
                user.EmailConfirmed));
    }
}

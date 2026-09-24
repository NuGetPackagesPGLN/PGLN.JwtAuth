using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Security;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler
    : ICommandHandler<
        ForgotPasswordCommand,
        Result>
{
    private readonly IUserRepository _userRepository;

    private readonly IPasswordResetTokenRepository
        _passwordResetTokenRepository;

    private readonly IPasswordResetTokenGenerator
        _passwordResetTokenGenerator;

    private readonly ITokenHasher _tokenHasher;

    private readonly IIntegrationEventPublisher
        _integrationEventPublisher;

    private readonly IUnitOfWork _unitOfWork;

    private readonly IClock _clock;

    private readonly PasswordResetOptions
        _options;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IPasswordResetTokenGenerator passwordResetTokenGenerator,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock,
        PasswordResetOptions options)
    {
        ArgumentNullException.ThrowIfNull(
            userRepository);

        ArgumentNullException.ThrowIfNull(
            passwordResetTokenRepository);

        ArgumentNullException.ThrowIfNull(
            passwordResetTokenGenerator);

        ArgumentNullException.ThrowIfNull(
            tokenHasher);

        ArgumentNullException.ThrowIfNull(
            integrationEventPublisher);

        ArgumentNullException.ThrowIfNull(
            unitOfWork);

        ArgumentNullException.ThrowIfNull(
            clock);

        ArgumentNullException.ThrowIfNull(
            options);

        options.Validate();

        _userRepository =
            userRepository;

        _passwordResetTokenRepository =
            passwordResetTokenRepository;

        _passwordResetTokenGenerator =
            passwordResetTokenGenerator;

        _tokenHasher =
            tokenHasher;

        _integrationEventPublisher =
            integrationEventPublisher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;

        _options =
            options;
    }

    public async Task<Result> HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var email =
            Email.Create(
                command.Email);

        var user =
            await _userRepository
                .GetByNormalizedEmailAsync(
                    email.NormalizedValue,
                    cancellationToken);

        // Deliberately return success for unknown accounts.
        //
        // This prevents callers from using the endpoint
        // to discover which email addresses are registered.
        if (user is null)
        {
            return Result.Success();
        }

        var now =
            _clock.UtcNow;

        // Invalidate all currently-active password-reset
        // tokens before issuing a replacement.
        //
        // This gives us a single-active-token policy.
        var activeTokens =
            await _passwordResetTokenRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    now,
                    cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.MarkAsUsed(
                now);
        }

        var rawResetToken =
            _passwordResetTokenGenerator
                .Generate();

        var resetTokenHash =
            _tokenHasher.Hash(
                rawResetToken);

        var resetToken =
            PasswordResetToken.Create(
                PasswordResetTokenId.New(),
                user.Id,
                resetTokenHash,
                now,
                now.Add(
                    _options.TokenLifetime));

        await _passwordResetTokenRepository
            .AddAsync(
                resetToken,
                cancellationToken);

        var resetRequested =
            new PasswordResetRequested(
                Guid.NewGuid(),
                user.Id,
                user.Email.Value,
                rawResetToken,
                now);

        await _integrationEventPublisher
            .PublishAsync(
                resetRequested,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}


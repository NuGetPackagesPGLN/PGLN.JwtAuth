using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.EmailChangeTokens;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ChangeEmail;

public sealed class RequestEmailChangeCommandHandler
    : ICommandHandler<
        RequestEmailChangeCommand,
        Result>
{
    private readonly IUserRepository
        _userRepository;

    private readonly IEmailChangeTokenRepository
        _emailChangeTokenRepository;

    private readonly IPasswordHasher
        _passwordHasher;

    private readonly IVerificationTokenGenerator
        _verificationTokenGenerator;

    private readonly ITokenHasher
        _tokenHasher;

    private readonly IIntegrationEventPublisher
        _integrationEventPublisher;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IClock
        _clock;

    private readonly EmailVerificationOptions
        _emailVerificationOptions;

    public RequestEmailChangeCommandHandler(
        IUserRepository userRepository,
        IEmailChangeTokenRepository emailChangeTokenRepository,
        IPasswordHasher passwordHasher,
        IVerificationTokenGenerator verificationTokenGenerator,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock,
        EmailVerificationOptions emailVerificationOptions)
    {
        ArgumentNullException.ThrowIfNull(
            userRepository);

        ArgumentNullException.ThrowIfNull(
            emailChangeTokenRepository);

        ArgumentNullException.ThrowIfNull(
            passwordHasher);

        ArgumentNullException.ThrowIfNull(
            verificationTokenGenerator);

        ArgumentNullException.ThrowIfNull(
            tokenHasher);

        ArgumentNullException.ThrowIfNull(
            integrationEventPublisher);

        ArgumentNullException.ThrowIfNull(
            unitOfWork);

        ArgumentNullException.ThrowIfNull(
            clock);

        ArgumentNullException.ThrowIfNull(
            emailVerificationOptions);

        emailVerificationOptions.Validate();

        _userRepository =
            userRepository;

        _emailChangeTokenRepository =
            emailChangeTokenRepository;

        _passwordHasher =
            passwordHasher;

        _verificationTokenGenerator =
            verificationTokenGenerator;

        _tokenHasher =
            tokenHasher;

        _integrationEventPublisher =
            integrationEventPublisher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;

        _emailVerificationOptions =
            emailVerificationOptions;
    }

    public async Task<Result> HandleAsync(
        RequestEmailChangeCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var user =
            await _userRepository
                .GetByIdAsync(
                    command.UserId,
                    cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                RequestEmailChangeErrors.UserNotFound);
        }

        var currentPasswordIsValid =
            _passwordHasher.Verify(
                command.CurrentPassword,
                user.PasswordHash);

        if (!currentPasswordIsValid)
        {
            return Result.Failure(
                RequestEmailChangeErrors.InvalidCurrentPassword);
        }

        Email newEmail;

        try
        {
            newEmail =
                Email.Create(
                    command.NewEmail);
        }
        catch (ArgumentException)
        {
            return Result.Failure(
                RequestEmailChangeErrors.InvalidEmail);
        }

        if (newEmail.NormalizedValue ==
            user.Email.NormalizedValue)
        {
            return Result.Failure(
                RequestEmailChangeErrors.SameEmail);
        }

        var emailAlreadyInUse =
            await _userRepository
                .ExistsByNormalizedEmailAsync(
                    newEmail.NormalizedValue,
                    cancellationToken);

        if (emailAlreadyInUse)
        {
            return Result.Failure(
                RequestEmailChangeErrors.EmailAlreadyInUse);
        }

        var now =
            _clock.UtcNow;

        var activeTokens =
            await _emailChangeTokenRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(
                now);
        }

        var rawVerificationToken =
            _verificationTokenGenerator.Generate();

        var verificationTokenHash =
            _tokenHasher.Hash(
                rawVerificationToken);

        var emailChangeToken =
            EmailChangeToken.Create(
                EmailChangeTokenId.New(),
                user.Id,
                newEmail,
                verificationTokenHash,
                now,
                now.Add(
                    _emailVerificationOptions.TokenLifetime));

        await _emailChangeTokenRepository
            .AddAsync(
                emailChangeToken,
                cancellationToken);

        var confirmationRequested =
            new EmailChangeConfirmationRequested(
                Guid.NewGuid(),
                user.Id,
                newEmail.Value,
                rawVerificationToken,
                now);

        await _integrationEventPublisher
            .PublishAsync(
                confirmationRequested,
                cancellationToken);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

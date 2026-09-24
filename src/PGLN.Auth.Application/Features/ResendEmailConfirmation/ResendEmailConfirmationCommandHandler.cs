using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Features.ResendEmailConfirmation;

internal sealed class ResendEmailConfirmationCommandHandler
    : ICommandHandler<
        ResendEmailConfirmationCommand,
        Result>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
    private readonly IVerificationTokenGenerator _verificationTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly EmailVerificationOptions _emailVerificationOptions;

    public ResendEmailConfirmationCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository verificationTokenRepository,
        IVerificationTokenGenerator verificationTokenGenerator,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock,
        EmailVerificationOptions emailVerificationOptions)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(verificationTokenRepository);
        ArgumentNullException.ThrowIfNull(verificationTokenGenerator);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(emailVerificationOptions);

        emailVerificationOptions.Validate();

        _userRepository = userRepository;
        _verificationTokenRepository = verificationTokenRepository;
        _verificationTokenGenerator = verificationTokenGenerator;
        _tokenHasher = tokenHasher;
        _integrationEventPublisher = integrationEventPublisher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _emailVerificationOptions = emailVerificationOptions;
    }

    public async Task<Result> HandleAsync(
        ResendEmailConfirmationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email =
            Email.Create(command.Email);

        var user =
            await _userRepository.GetByNormalizedEmailAsync(
                email.NormalizedValue,
                cancellationToken);

        // Deliberately return success.
        //
        // The public API must not reveal whether an account
        // exists for the supplied email address.
        if (user is null)
        {
            return Result.Success();
        }

        // Same enumeration protection applies here.
        //
        // A caller should not be able to determine that an
        // account exists merely because it is already confirmed.
        if (user.EmailConfirmed)
        {
            return Result.Success();
        }

        var now =
            _clock.UtcNow;

        var activeTokens =
            await _verificationTokenRepository
                .GetActiveByUserIdAsync(
                    user.Id,
                    cancellationToken);

        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(now);
        }

        var rawVerificationToken =
            _verificationTokenGenerator.Generate();

        var verificationTokenHash =
            _tokenHasher.Hash(
                rawVerificationToken);

        var verificationToken =
            EmailVerificationToken.Create(
                EmailVerificationTokenId.New(),
                user.Id,
                verificationTokenHash,
                now,
                now.Add(
                    _emailVerificationOptions.TokenLifetime));

        await _verificationTokenRepository.AddAsync(
            verificationToken,
            cancellationToken);

        var confirmationRequested =
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                user.Id,
                user.Email.Value,
                rawVerificationToken,
                now);

        await _integrationEventPublisher.PublishAsync(
            confirmationRequested,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result.Success();
    }
}

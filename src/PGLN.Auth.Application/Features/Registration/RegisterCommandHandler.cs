using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;

namespace PGLN.Auth.Application.Features.Registration;

internal sealed class RegisterCommandHandler
    : ICommandHandler<
        RegisterCommand,
        Result<RegisterResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailVerificationTokenRepository _verificationTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IVerificationTokenGenerator _verificationTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly EmailVerificationOptions _emailVerificationOptions;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IEmailVerificationTokenRepository verificationTokenRepository,
        IPasswordHasher passwordHasher,
        IVerificationTokenGenerator verificationTokenGenerator,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock,
        EmailVerificationOptions emailVerificationOptions)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(verificationTokenRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(verificationTokenGenerator);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(emailVerificationOptions);

        emailVerificationOptions.Validate();

        _userRepository = userRepository;
        _verificationTokenRepository = verificationTokenRepository;
        _passwordHasher = passwordHasher;
        _verificationTokenGenerator = verificationTokenGenerator;
        _tokenHasher = tokenHasher;
        _integrationEventPublisher = integrationEventPublisher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _emailVerificationOptions = emailVerificationOptions;
    }

    public async Task<Result<RegisterResult>> HandleAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email =
            Email.Create(command.Email);

        var emailAlreadyExists =
            await _userRepository.ExistsByNormalizedEmailAsync(
                email.NormalizedValue,
                cancellationToken);

        if (emailAlreadyExists)
        {
            return Result<RegisterResult>.Failure(
                RegistrationErrors.EmailAlreadyExists);
        }

        var passwordHash =
            _passwordHasher.Hash(
                command.Password);

        var now =
            _clock.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                email,
                passwordHash,
                now);

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

        await _userRepository.AddAsync(
            user,
            cancellationToken);

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

        return Result<RegisterResult>.Success(
            new RegisterResult(
                user.Id,
                user.Email.Value,
                user.EmailConfirmed));
    }
}

using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.Login;

public sealed class LoginCommandHandler
    : ICommandHandler<
        LoginCommand,
        Result<LoginResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILoginAttemptRepository _loginAttemptRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly IStepUpChallengeRepository _stepUpChallengeRepository;
    private readonly IStepUpCodeGenerator _stepUpCodeGenerator;
    private readonly IStepUpCodeProtector _stepUpCodeProtector;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IIntegrationEventPublisher
        _integrationEventPublisher;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly RefreshTokenOptions _refreshTokenOptions;
    private readonly AccountLockoutOptions _accountLockoutOptions;
    private readonly LoginEmailThrottleOptions _loginEmailThrottleOptions;
    private readonly StepUpChallengeOptions _stepUpChallengeOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ILoginAttemptRepository loginAttemptRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAuthSessionRepository authSessionRepository,
        ITrustedDeviceRepository trustedDeviceRepository,
        IStepUpChallengeRepository stepUpChallengeRepository,
        IStepUpCodeGenerator stepUpCodeGenerator,
        IStepUpCodeProtector stepUpCodeProtector,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IIntegrationEventPublisher integrationEventPublisher,
        IUnitOfWork unitOfWork,
        IClock clock,
        RefreshTokenOptions refreshTokenOptions,
        AccountLockoutOptions accountLockoutOptions,
        LoginEmailThrottleOptions loginEmailThrottleOptions,
        StepUpChallengeOptions stepUpChallengeOptions)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(loginAttemptRepository);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(authSessionRepository);
        ArgumentNullException.ThrowIfNull(trustedDeviceRepository);
        ArgumentNullException.ThrowIfNull(stepUpChallengeRepository);
        ArgumentNullException.ThrowIfNull(stepUpCodeGenerator);
        ArgumentNullException.ThrowIfNull(stepUpCodeProtector);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(accessTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(integrationEventPublisher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(refreshTokenOptions);
        ArgumentNullException.ThrowIfNull(accountLockoutOptions);
        ArgumentNullException.ThrowIfNull(loginEmailThrottleOptions);
        ArgumentNullException.ThrowIfNull(stepUpChallengeOptions);

        refreshTokenOptions.Validate();
        accountLockoutOptions.Validate();
        loginEmailThrottleOptions.Validate();

        _userRepository = userRepository;
        _loginAttemptRepository = loginAttemptRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _authSessionRepository = authSessionRepository;
        _trustedDeviceRepository = trustedDeviceRepository;
        _stepUpChallengeRepository = stepUpChallengeRepository;
        _stepUpCodeGenerator = stepUpCodeGenerator;
        _stepUpCodeProtector = stepUpCodeProtector;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _integrationEventPublisher = integrationEventPublisher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _refreshTokenOptions = refreshTokenOptions;
        _accountLockoutOptions = accountLockoutOptions;
        _loginEmailThrottleOptions = loginEmailThrottleOptions;
        _stepUpChallengeOptions = stepUpChallengeOptions;
    }

    public async Task<Result<LoginResult>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var email =
            Email.Create(
                command.Email);

        var now =
            _clock.UtcNow;

        var throttleWindowStart =
            now.Subtract(
                _loginEmailThrottleOptions.Window);

        var recentFailedAttempts =
            await _loginAttemptRepository
                .CountFailedAttemptsAsync(
                    email.NormalizedValue,
                    throttleWindowStart,
                    cancellationToken);

        if (recentFailedAttempts >=
            _loginEmailThrottleOptions.MaxFailedAttempts)
        {
            return Result<LoginResult>.Failure(
                LoginErrors.TooManyAttempts);
        }

        var user =
            await _userRepository
                .GetByNormalizedEmailAsync(
                    email.NormalizedValue,
                    cancellationToken);

        if (user is null)
        {
            await RecordFailureAsync(
                email.NormalizedValue,
                null,
                LoginFailureReason.InvalidCredentials,
                now,
                cancellationToken);

            return Result<LoginResult>.Failure(
                LoginErrors.InvalidCredentials);
        }

        if (user.IsLockedOut(now))
        {
            await RecordFailureAsync(
                email.NormalizedValue,
                user.Id,
                LoginFailureReason.InvalidCredentials,
                now,
                cancellationToken);

            return Result<LoginResult>.Failure(
                LoginErrors.AccountLocked);
        }

        var passwordIsValid =
            _passwordHasher.Verify(
                command.Password,
                user.PasswordHash);

        if (!passwordIsValid)
        {
            if (user.LastFailedLoginAtUtc.HasValue &&
                now - user.LastFailedLoginAtUtc.Value >
                _accountLockoutOptions.FailureWindow)
            {
                user.ResetFailedLoginAttempts();
            }

            user.RecordFailedLoginAttempt(
                now);

            if (user.FailedLoginAttempts >=
                _accountLockoutOptions.MaxFailedAttempts)
            {
                var lockedUntilUtc =
                    now.Add(
                        _accountLockoutOptions.LockoutDuration);

                user.LockOutUntil(
                    lockedUntilUtc);

                var notificationRequested =
                    new AccountLockedNotificationRequested(
                        Guid.NewGuid(),
                        user.Id,
                        user.Email.Value,
                        lockedUntilUtc,
                        now);

                await _integrationEventPublisher
                    .PublishAsync(
                        notificationRequested,
                        cancellationToken);
            }

            await RecordFailureAsync(
                email.NormalizedValue,
                user.Id,
                LoginFailureReason.InvalidCredentials,
                now,
                cancellationToken);

            return Result<LoginResult>.Failure(
                LoginErrors.InvalidCredentials);
        }

        if (!user.EmailConfirmed)
        {
            await RecordFailureAsync(
                email.NormalizedValue,
                user.Id,
                LoginFailureReason.EmailNotConfirmed,
                now,
                cancellationToken);

            return Result<LoginResult>.Failure(
                LoginErrors.EmailNotConfirmed);
        }

        user.ResetFailedLoginAttempts();

        var hasSeenDevice =
            await _authSessionRepository
                .HasSeenDeviceAsync(
                    user.Id,
                    command.DeviceIdHash,
                    cancellationToken);

        var isNewDevice =
            !hasSeenDevice;

        var trustedDevice =
            await _trustedDeviceRepository
                .GetByUserAndDeviceHashAsync(
                    user.Id,
                    command.DeviceIdHash,
                    cancellationToken);

        var deviceIsTrusted =
            trustedDevice is not null &&
            trustedDevice.IsTrusted;

        if (!deviceIsTrusted)
        {
            var activeChallenge =
                await _stepUpChallengeRepository
                    .GetActiveByUserAndDeviceHashAsync(
                        user.Id,
                        command.DeviceIdHash,
                        now,
                        cancellationToken);

            if (activeChallenge is not null)
            {
                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return Result<LoginResult>.Success(
                    LoginResult.StepUpRequired(
                        user.Id.Value,
                        user.Email.Value,
                        activeChallenge.Id.Value));
            }

            var stepUpCode =
                _stepUpCodeGenerator.Generate();

            var protectedCode =
                _stepUpCodeProtector.Protect(
                    stepUpCode);

            var challenge =
                StepUpChallenge.Create(
                    StepUpChallengeId.New(),
                    user.Id,
                    command.DeviceIdHash,
                    command.DeviceName,
                    protectedCode,
                    now,
                    now.Add(
                        _stepUpChallengeOptions.Lifetime));

            await _stepUpChallengeRepository
                .AddAsync(
                    challenge,
                    cancellationToken);

            var verificationCodeRequested =
                new StepUpVerificationCodeRequested(
                    Guid.NewGuid(),
                    user.Id,
                    challenge.Id,
                    user.Email.Value,
                    stepUpCode,
                    command.DeviceName,
                    command.IpAddress,
                    command.UserAgent,
                    challenge.ExpiresAtUtc,
                    now);

            await _integrationEventPublisher
                .PublishAsync(
                    verificationCodeRequested,
                    cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return Result<LoginResult>.Success(
                LoginResult.StepUpRequired(
                    user.Id.Value,
                    user.Email.Value,
                    challenge.Id.Value));
        }

        var session =
            await _authSessionRepository
                .GetActiveByDeviceIdHashAsync(
                    user.Id,
                    command.DeviceIdHash,
                    cancellationToken);

        if (session is null)
        {
            session =
                AuthSession.Create(
                    AuthSessionId.New(),
                    user.Id,
                    command.DeviceIdHash,
                    command.DeviceName,
                    command.IpAddress,
                    command.UserAgent,
                    now);

            await _authSessionRepository
                .AddAsync(
                    session,
                    cancellationToken);
        }
        else
        {
            session.Touch(
                now,
                command.IpAddress,
                command.UserAgent);
        }
        session.TrustDevice();

        var rawRefreshToken =
            _refreshTokenGenerator.Generate();

        var refreshTokenHash =
            _tokenHasher.Hash(
                rawRefreshToken);

        var refreshToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                RefreshTokenFamilyId.New(),
                user.Id,
                session.Id,
                refreshTokenHash,
                now,
                now.Add(
                    _refreshTokenOptions.TokenLifetime));

        await _refreshTokenRepository.AddAsync(
            refreshToken,
            cancellationToken);

        var loginAttempt =
            LoginAttempt.Successful(
                email.NormalizedValue,
                user.Id,
                now);

        await _loginAttemptRepository.AddAsync(
            loginAttempt,
            cancellationToken);

        if (isNewDevice)
        {
            var notificationRequested =
                new NewDeviceLoginNotificationRequested(
                    Guid.NewGuid(),
                    user.Id,
                    user.Email.Value,
                    command.DeviceIdHash,
                    command.DeviceName,
                    command.IpAddress,
                    command.UserAgent,
                    now);

            await _integrationEventPublisher
                .PublishAsync(
                    notificationRequested,
                    cancellationToken);
        }

        var accessToken =
            _accessTokenGenerator.Generate(
                user,
                session.Id,
                now);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<LoginResult>.Success(
            LoginResult.AuthenticationComplete(
                user.Id.Value,
                user.Email.Value,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                rawRefreshToken,
                refreshToken.ExpiresAtUtc));
    }

    private async Task RecordFailureAsync(
        string email,
        UserId? userId,
        LoginFailureReason reason,
        DateTimeOffset attemptedAtUtc,
        CancellationToken cancellationToken)
    {
        var attempt =
            LoginAttempt.Failed(
                email,
                userId,
                reason,
                attemptedAtUtc);

        await _loginAttemptRepository.AddAsync(
            attempt,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);
    }
}


















using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.TrustedDevices;

namespace PGLN.Auth.Application.Features.StepUp.VerifyStepUpChallenge;

internal sealed class VerifyStepUpChallengeCommandHandler
    : ICommandHandler<
        VerifyStepUpChallengeCommand,
        Result<VerifyStepUpChallengeResult>>
{
    private readonly IStepUpChallengeRepository _stepUpChallengeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ITrustedDeviceRepository _trustedDeviceRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ILoginAttemptRepository _loginAttemptRepository;
    private readonly IStepUpCodeProtector _stepUpCodeProtector;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly RefreshTokenOptions _refreshTokenOptions;
    private readonly StepUpChallengeOptions _stepUpChallengeOptions;

    public VerifyStepUpChallengeCommandHandler(
        IStepUpChallengeRepository stepUpChallengeRepository,
        IUserRepository userRepository,
        IAuthSessionRepository authSessionRepository,
        ITrustedDeviceRepository trustedDeviceRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ILoginAttemptRepository loginAttemptRepository,
        IStepUpCodeProtector stepUpCodeProtector,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        RefreshTokenOptions refreshTokenOptions,
        StepUpChallengeOptions stepUpChallengeOptions)
    {
        _stepUpChallengeRepository = stepUpChallengeRepository;
        _userRepository = userRepository;
        _authSessionRepository = authSessionRepository;
        _trustedDeviceRepository = trustedDeviceRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _loginAttemptRepository = loginAttemptRepository;
        _stepUpCodeProtector = stepUpCodeProtector;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _refreshTokenOptions = refreshTokenOptions;
        _stepUpChallengeOptions = stepUpChallengeOptions;
    }

    public async Task<Result<VerifyStepUpChallengeResult>> HandleAsync(
        VerifyStepUpChallengeCommand command,
        CancellationToken cancellationToken = default)
    {
        var now =
            _clock.UtcNow;

        var challenge =
            await _stepUpChallengeRepository
                .GetByIdAsync(
                    new StepUpChallengeId(
                        command.ChallengeId),
                    cancellationToken);

        if (challenge is null)
        {
            return Result<VerifyStepUpChallengeResult>.Failure(
                new Error(
                    "StepUpChallenge.NotFound",
                    "The step-up challenge was not found."));
        }

        if (challenge.IsVerified)
        {
            return Result<VerifyStepUpChallengeResult>.Failure(
                new Error(
                    "StepUpChallenge.AlreadyVerified",
                    "The step-up challenge has already been verified."));
        }

        if (challenge.IsExpired(
                now))
        {
            return Result<VerifyStepUpChallengeResult>.Failure(
                new Error(
                    "StepUpChallenge.Expired",
                    "The step-up challenge has expired."));
        }

        if (challenge.FailedAttempts >=
            _stepUpChallengeOptions.MaxFailedAttempts)
        {
            return Result<VerifyStepUpChallengeResult>.Failure(
                new Error(
                    "StepUpChallenge.MaximumAttemptsExceeded",
                    "The maximum number of verification attempts has been exceeded."));
        }

        var codeIsValid =
            _stepUpCodeProtector.Verify(
                command.Code,
                challenge.CodeHash);

        if (!codeIsValid)
        {
            challenge.RecordFailedAttempt();

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return Result<VerifyStepUpChallengeResult>.Failure(
                new Error(
                    "StepUpChallenge.InvalidCode",
                    "The verification code is invalid."));
        }

        var user =
            await _userRepository
                .GetByIdAsync(
                    challenge.UserId,
                    cancellationToken);

        if (user is null)
        {
            return Result<VerifyStepUpChallengeResult>.Failure(
                new Error(
                    "User.NotFound",
                    "The user associated with the challenge was not found."));
        }

        challenge.MarkVerified(
            now);

        if (command.RememberDevice)
        {
            var trustedDevice =
                await _trustedDeviceRepository
                    .GetByUserAndDeviceHashAsync(
                        user.Id,
                        challenge.DeviceIdHash,
                        cancellationToken);

            if (trustedDevice is null)
            {
                trustedDevice =
                    TrustedDevice.Create(
                        TrustedDeviceId.New(),
                        user.Id,
                        challenge.DeviceIdHash,
                        challenge.DeviceName,
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
                    challenge.DeviceName);
            }
        }

        var session =
            await _authSessionRepository
                .GetActiveByDeviceIdHashAsync(
                    user.Id,
                    challenge.DeviceIdHash,
                    cancellationToken);

        if (session is null)
        {
            session =
                AuthSession.Create(
                    AuthSessionId.New(),
                    user.Id,
                    challenge.DeviceIdHash,
                    challenge.DeviceName,
                    null,
                    null,
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
                null,
                null);
        }

        if (command.RememberDevice)
        {
            session.TrustDevice();
        }

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

        await _refreshTokenRepository
            .AddAsync(
                refreshToken,
                cancellationToken);

        var accessToken =
            _accessTokenGenerator.Generate(
                user,
                session.Id,
                now);

        var successfulLoginAttempt =
            LoginAttempt.Successful(
                user.NormalizedEmail,
                user.Id,
                now);

        await _loginAttemptRepository
            .AddAsync(
                successfulLoginAttempt,
                cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<VerifyStepUpChallengeResult>.Success(
            new VerifyStepUpChallengeResult(
                user.Id.Value,
                user.Email.Value,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                rawRefreshToken,
                refreshToken.ExpiresAtUtc));
    }
}

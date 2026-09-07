using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.RefreshTokens;
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
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ILoginAttemptRepository loginAttemptRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        RefreshTokenOptions refreshTokenOptions)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(loginAttemptRepository);
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(accessTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(refreshTokenOptions);

        refreshTokenOptions.Validate();

        _userRepository = userRepository;
        _loginAttemptRepository = loginAttemptRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _accessTokenGenerator = accessTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenHasher = tokenHasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _refreshTokenOptions = refreshTokenOptions;
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

        var passwordIsValid =
            _passwordHasher.Verify(
                command.Password,
                user.PasswordHash);

        if (!passwordIsValid)
        {
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

        var rawRefreshToken =
            _refreshTokenGenerator.Generate();

        var refreshTokenHash =
            _tokenHasher.Hash(
                rawRefreshToken);

        var refreshToken =
            RefreshToken.Create(
                RefreshTokenId.New(),
                user.Id,
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

        var accessToken =
            _accessTokenGenerator.Generate(
                user,
                now);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<LoginResult>.Success(
            new LoginResult(
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

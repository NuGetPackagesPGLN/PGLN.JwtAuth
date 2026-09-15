using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.ExternalAuthentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Application.Configuration;
using PGLN.Auth.Domain.ExternalLogins;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Features.ExternalAuthentication.CompleteExternalLogin;

public sealed class CompleteExternalLoginCommandHandler
    : ICommandHandler<
        CompleteExternalLoginCommand,
        Result<CompleteExternalLoginResult>>
{
    private readonly IExternalIdentityProviderResolver
        _externalIdentityProviderResolver;

    private readonly IExternalAuthenticationStateProtector
        _stateProtector;

    private readonly IExternalLoginRepository
        _externalLoginRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly IAuthSessionRepository
        _authSessionRepository;

    private readonly IRefreshTokenRepository
        _refreshTokenRepository;

    private readonly IAccessTokenGenerator
        _accessTokenGenerator;

    private readonly IRefreshTokenGenerator
        _refreshTokenGenerator;

    private readonly ITokenHasher
        _tokenHasher;

    private readonly IUnitOfWork
        _unitOfWork;

    private readonly IClock
        _clock;

    private readonly RefreshTokenOptions
        _refreshTokenOptions;

    public CompleteExternalLoginCommandHandler(
        IExternalIdentityProviderResolver externalIdentityProviderResolver,
        IExternalAuthenticationStateProtector stateProtector,
        IExternalLoginRepository externalLoginRepository,
        IUserRepository userRepository,
        IAuthSessionRepository authSessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IAccessTokenGenerator accessTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        RefreshTokenOptions refreshTokenOptions)
    {
        ArgumentNullException.ThrowIfNull(
            externalIdentityProviderResolver);

        ArgumentNullException.ThrowIfNull(
            stateProtector);

        ArgumentNullException.ThrowIfNull(
            externalLoginRepository);

        ArgumentNullException.ThrowIfNull(
            userRepository);

        ArgumentNullException.ThrowIfNull(
            authSessionRepository);

        ArgumentNullException.ThrowIfNull(
            refreshTokenRepository);

        ArgumentNullException.ThrowIfNull(
            accessTokenGenerator);

        ArgumentNullException.ThrowIfNull(
            refreshTokenGenerator);

        ArgumentNullException.ThrowIfNull(
            tokenHasher);

        ArgumentNullException.ThrowIfNull(
            unitOfWork);

        ArgumentNullException.ThrowIfNull(
            clock);

        ArgumentNullException.ThrowIfNull(
            refreshTokenOptions);

        refreshTokenOptions.Validate();

        _externalIdentityProviderResolver =
            externalIdentityProviderResolver;

        _stateProtector =
            stateProtector;

        _externalLoginRepository =
            externalLoginRepository;

        _userRepository =
            userRepository;

        _authSessionRepository =
            authSessionRepository;

        _refreshTokenRepository =
            refreshTokenRepository;

        _accessTokenGenerator =
            accessTokenGenerator;

        _refreshTokenGenerator =
            refreshTokenGenerator;

        _tokenHasher =
            tokenHasher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;

        _refreshTokenOptions =
            refreshTokenOptions;
    }

    public async Task<Result<CompleteExternalLoginResult>> HandleAsync(
        CompleteExternalLoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        ExternalAuthenticationState authenticationState;

        try
        {
            authenticationState =
                _stateProtector.Unprotect(
                    command.State);
        }
        catch
        {
            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.InvalidState);
        }

        if (!string.Equals(
                authenticationState.Provider,
                command.Provider.ToString(),
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                authenticationState.RedirectUri,
                command.RedirectUri,
                StringComparison.Ordinal))
        {
            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.InvalidState);
        }

        if (!string.IsNullOrWhiteSpace(
                command.ProviderError))
        {
            if (string.Equals(
                    command.ProviderError,
                    "access_denied",
                    StringComparison.Ordinal))
            {
                return Result<CompleteExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.AuthorizationDenied);
            }

            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.ProviderRejected);
        }

        if (string.IsNullOrWhiteSpace(
                command.AuthorizationCode))
        {
            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.AuthorizationCodeMissing);
        }

        IExternalIdentityProvider provider;

        try
        {
            provider =
                _externalIdentityProviderResolver
                    .GetProvider(
                        command.Provider);
        }
        catch (InvalidOperationException)
        {
            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.ProviderNotSupported);
        }

        ExternalIdentity identity;

        try
        {
            identity =
                await provider.GetIdentityAsync(
                    command.AuthorizationCode,
                    command.RedirectUri,
                    authenticationState.CodeVerifier,
                    cancellationToken);
        }
        catch (ExternalIdentityProviderException)
        {
            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.ProviderFailure);
        }

        if (identity.Provider != command.Provider ||
            string.IsNullOrWhiteSpace(
                identity.ProviderSubject))
        {
            return Result<CompleteExternalLoginResult>.Failure(
                ExternalAuthenticationErrors.IdentityInvalid);
        }

        var now =
            _clock.UtcNow;

        var externalLogin =
            await _externalLoginRepository
                .GetByProviderAndSubjectAsync(
                    identity.Provider,
                    identity.ProviderSubject,
                    cancellationToken);

        User user;
        var isNewUser = false;

        if (externalLogin is not null)
        {
            var linkedUser =
                await _userRepository
                    .GetByIdAsync(
                        externalLogin.UserId,
                        cancellationToken);

            if (linkedUser is null)
            {
                return Result<CompleteExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.IdentityInvalid);
            }

            user =
                linkedUser;

            externalLogin.RecordLogin(
                now,
                identity.Email);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(
                    identity.Email))
            {
                return Result<CompleteExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.EmailRequired);
            }

            var identityEmail =
                identity.Email;

            if (!identity.EmailVerified)
            {
                return Result<CompleteExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.EmailNotVerified);
            }

            Email email;

            try
            {
                email =
                    Email.Create(
                        identityEmail);
            }
            catch (ArgumentException)
            {
                return Result<CompleteExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.IdentityInvalid);
            }

            var existingUser =
                await _userRepository
                    .GetByNormalizedEmailAsync(
                        email.NormalizedValue,
                        cancellationToken);

            if (existingUser is not null)
            {
                return Result<CompleteExternalLoginResult>.Failure(
                    ExternalAuthenticationErrors.AccountLinkRequired);
            }

            user =
                User.RegisterExternal(
                    UserId.New(),
                    email,
                    now);

            externalLogin =
                ExternalLogin.Create(
                    user.Id,
                    identity.Provider,
                    identity.ProviderSubject,
                    identityEmail,
                    now);

            await _userRepository.AddAsync(
                user,
                cancellationToken);

            await _externalLoginRepository.AddAsync(
                externalLogin,
                cancellationToken);

            isNewUser = true;
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

            await _authSessionRepository.AddAsync(
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

        var accessToken =
            _accessTokenGenerator.Generate(
                user,
                session.Id,
                now);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CompleteExternalLoginResult>.Success(
            new CompleteExternalLoginResult(
                user.Id.Value,
                user.Email.Value,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                rawRefreshToken,
                refreshToken.ExpiresAtUtc,
                isNewUser));
    }
}








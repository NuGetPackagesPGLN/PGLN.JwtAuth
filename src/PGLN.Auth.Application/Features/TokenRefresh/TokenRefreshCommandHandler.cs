using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;
using PGLN.Auth.Domain.RefreshTokens;

namespace PGLN.Auth.Application.Features.TokenRefresh;

internal sealed class TokenRefreshCommandHandler
    : ICommandHandler<
        TokenRefreshCommand,
        Result<TokenRefreshResult>>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly RefreshTokenOptions _refreshTokenOptions;

    public TokenRefreshCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuthSessionRepository authSessionRepository,
        IUserRepository userRepository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IAccessTokenGenerator accessTokenGenerator,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock,
        RefreshTokenOptions refreshTokenOptions)
    {
        ArgumentNullException.ThrowIfNull(refreshTokenRepository);
        ArgumentNullException.ThrowIfNull(authSessionRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(accessTokenGenerator);
        ArgumentNullException.ThrowIfNull(tokenHasher);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(refreshTokenOptions);

        refreshTokenOptions.Validate();

        _refreshTokenRepository =
            refreshTokenRepository;

        _authSessionRepository =
            authSessionRepository;

        _userRepository =
            userRepository;

        _refreshTokenGenerator =
            refreshTokenGenerator;

        _accessTokenGenerator =
            accessTokenGenerator;

        _tokenHasher =
            tokenHasher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;

        _refreshTokenOptions =
            refreshTokenOptions;
    }

    public async Task<Result<TokenRefreshResult>> HandleAsync(
        TokenRefreshCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now =
            _clock.UtcNow;

        var presentedTokenHash =
            _tokenHasher.Hash(
                command.RefreshToken);

        var existingToken =
            await _refreshTokenRepository
                .GetByTokenHashAsync(
                    presentedTokenHash,
                    cancellationToken);

        if (existingToken is null)
        {
            return Result<TokenRefreshResult>.Failure(
                TokenRefreshErrors.InvalidToken);
        }

        if (existingToken.IsRevoked)
        {
            if (
                existingToken.RevocationReason ==
                "Rotated")
            {
                var familyTokens =
                    await _refreshTokenRepository
                        .GetByFamilyIdAsync(
                            existingToken.FamilyId,
                            cancellationToken);

                var stateChanged =
                    false;

                foreach (var familyToken in familyTokens)
                {
                    if (!familyToken.IsActive(now))
                    {
                        continue;
                    }

                    familyToken.Revoke(
                        now,
                        "RefreshTokenReuseDetected");

                    stateChanged =
                        true;
                }

                var compromisedSession =
                    await _authSessionRepository
                        .GetByIdAsync(
                            existingToken.SessionId,
                            cancellationToken);

                if (
                    compromisedSession is not null &&
                    compromisedSession.IsActive)
                {
                    compromisedSession.Revoke(
                        now,
                        "RefreshTokenReuseDetected");

                    stateChanged =
                        true;
                }

                if (stateChanged)
                {
                    await _unitOfWork
                        .SaveChangesAsync(
                            cancellationToken);
                }
            }

            return Result<TokenRefreshResult>.Failure(
                TokenRefreshErrors.RevokedToken);
        }

        if (existingToken.IsExpired(now))
        {
            return Result<TokenRefreshResult>.Failure(
                TokenRefreshErrors.ExpiredToken);
        }

        var session =
            await _authSessionRepository
                .GetByIdAsync(
                    existingToken.SessionId,
                    cancellationToken);

        if (session is null ||
            !session.IsActive)
        {
            return Result<TokenRefreshResult>.Failure(
                TokenRefreshErrors.RevokedToken);
        }

        var user =
            await _userRepository
                .GetByIdAsync(
                    existingToken.UserId,
                    cancellationToken);

        if (user is null)
        {
            return Result<TokenRefreshResult>.Failure(
                TokenRefreshErrors.UserNotFound);
        }

        var rawReplacementToken =
            _refreshTokenGenerator.Generate();

        var replacementTokenHash =
            _tokenHasher.Hash(
                rawReplacementToken);

        var replacement =
            RefreshToken.Create(
                RefreshTokenId.New(),
                existingToken.FamilyId,
                user.Id,
                existingToken.SessionId,
                replacementTokenHash,
                now,
                now.Add(
                    _refreshTokenOptions.TokenLifetime));

        existingToken.Rotate(
            replacement.Id,
            now);

        await _refreshTokenRepository
            .AddAsync(
                replacement,
                cancellationToken);

        var accessToken =
            _accessTokenGenerator.Generate(
                user,
                existingToken.SessionId,
                now);

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result<TokenRefreshResult>.Success(
            new TokenRefreshResult(
                user.Id.Value,
                user.Email.Value,
                accessToken.Token,
                accessToken.ExpiresAtUtc,
                rawReplacementToken,
                replacement.ExpiresAtUtc));
    }
}








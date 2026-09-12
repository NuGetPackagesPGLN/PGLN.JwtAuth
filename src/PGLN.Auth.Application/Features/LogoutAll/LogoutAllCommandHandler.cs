using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.LogoutAll;

public sealed class LogoutAllCommandHandler
    : ICommandHandler<LogoutAllCommand, Result>
{
    private const string RevocationReason =
        "LogoutAll";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LogoutAllCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuthSessionRepository authSessionRepository,
        ITokenHasher tokenHasher,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(
            refreshTokenRepository);

        ArgumentNullException.ThrowIfNull(
            authSessionRepository);

        ArgumentNullException.ThrowIfNull(
            tokenHasher);

        ArgumentNullException.ThrowIfNull(
            unitOfWork);

        ArgumentNullException.ThrowIfNull(
            clock);

        _refreshTokenRepository =
            refreshTokenRepository;

        _authSessionRepository =
            authSessionRepository;

        _tokenHasher =
            tokenHasher;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;
    }

    public async Task<Result> HandleAsync(
        LogoutAllCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var now =
            _clock.UtcNow;

        var tokenHash =
            _tokenHasher.Hash(
                command.RefreshToken);

        var anchorToken =
            await _refreshTokenRepository
                .GetByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

        if (anchorToken is null)
        {
            return Result.Failure(
                LogoutAllErrors.InvalidToken);
        }

        if (anchorToken.IsRevoked)
        {
            return Result.Failure(
                LogoutAllErrors.RevokedToken);
        }

        if (anchorToken.IsExpired(now))
        {
            return Result.Failure(
                LogoutAllErrors.ExpiredToken);
        }

        var userTokens =
            await _refreshTokenRepository
                .GetByUserIdAsync(
                    anchorToken.UserId,
                    cancellationToken);

        var userSessions =
            await _authSessionRepository
                .GetByUserIdAsync(
                    anchorToken.UserId,
                    cancellationToken);

        var changed =
            false;

        foreach (var refreshToken in userTokens)
        {
            if (!refreshToken.IsActive(now))
            {
                continue;
            }

            refreshToken.Revoke(
                now,
                RevocationReason);

            changed =
                true;
        }

        foreach (var session in userSessions)
        {
            if (!session.IsActive)
            {
                continue;
            }

            session.Revoke(
                now,
                RevocationReason);

            changed =
                true;
        }

        if (changed)
        {
            await _unitOfWork
                .SaveChangesAsync(
                    cancellationToken);
        }

        return Result.Success();
    }
}

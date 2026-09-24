using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Logout;

internal sealed class LogoutCommandHandler
    : ICommandHandler<
        LogoutCommand,
        Result>
{
    private const string RevocationReason =
        "Logout";

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly ITokenHasher _tokenHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public LogoutCommandHandler(
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
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            command);

        var tokenHash =
            _tokenHasher.Hash(
                command.RefreshToken);

        var refreshToken =
            await _refreshTokenRepository
                .GetByTokenHashAsync(
                    tokenHash,
                    cancellationToken);

        //
        // Logout is deliberately idempotent.
        //
        // We do not reveal whether:
        // - the token exists,
        // - it has already been revoked,
        // - it has expired.
        //
        if (refreshToken is null)
        {
            return Result.Success();
        }

        if (refreshToken.IsRevoked)
        {
            return Result.Success();
        }

        if (refreshToken.IsExpired(
                _clock.UtcNow))
        {
            return Result.Success();
        }

        var now =
            _clock.UtcNow;

        refreshToken.Revoke(
            now,
            RevocationReason);

        var session =
            await _authSessionRepository
                .GetByIdAsync(
                    refreshToken.SessionId,
                    cancellationToken);

        if (session is not null)
        {
            session.Revoke(
                now,
                RevocationReason);
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

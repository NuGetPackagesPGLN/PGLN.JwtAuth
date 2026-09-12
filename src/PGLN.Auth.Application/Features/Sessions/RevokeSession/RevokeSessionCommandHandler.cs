using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Sessions.RevokeSession;

public sealed class RevokeSessionCommandHandler
    : ICommandHandler<RevokeSessionCommand, Result>
{
    private static readonly Error SessionNotFoundError =
        new(
            "Sessions.NotFound",
            "The requested session was not found.");

    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeSessionCommandHandler(
        IAuthSessionRepository authSessionRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _authSessionRepository =
            authSessionRepository;

        _refreshTokenRepository =
            refreshTokenRepository;

        _unitOfWork =
            unitOfWork;

        _clock =
            clock;
    }

    public async Task<Result> HandleAsync(
        RevokeSessionCommand command,
        CancellationToken cancellationToken = default)
    {
        var session =
            await _authSessionRepository
                .GetByIdAsync(
                    command.SessionId,
                    cancellationToken);

        if (session is null ||
            session.UserId != command.UserId)
        {
            return Result.Failure(
                SessionNotFoundError);
        }

        var now =
            _clock.UtcNow;

        session.Revoke(
            now,
            "UserRevokedSession");

        var refreshTokens =
            await _refreshTokenRepository
                .GetBySessionIdAsync(
                    session.Id,
                    cancellationToken);

        foreach (var refreshToken in refreshTokens)
        {
            refreshToken.Revoke(
                now,
                "SessionRevoked");
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

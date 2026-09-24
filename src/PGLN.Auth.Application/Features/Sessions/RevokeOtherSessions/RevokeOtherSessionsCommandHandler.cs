using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Sessions.RevokeOtherSessions;

internal sealed class RevokeOtherSessionsCommandHandler
    : ICommandHandler<RevokeOtherSessionsCommand, Result>
{
    private static readonly Error CurrentSessionNotFoundError =
        new(
            "Sessions.CurrentSessionNotFound",
            "The current session was not found.");

    private readonly IAuthSessionRepository _authSessionRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeOtherSessionsCommandHandler(
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
        RevokeOtherSessionsCommand command,
        CancellationToken cancellationToken = default)
    {
        var currentSession =
            await _authSessionRepository
                .GetByIdAsync(
                    command.CurrentSessionId,
                    cancellationToken);

        if (currentSession is null ||
            currentSession.UserId != command.UserId ||
            currentSession.IsRevoked)
        {
            return Result.Failure(
                CurrentSessionNotFoundError);
        }

        var sessions =
            await _authSessionRepository
                .GetByUserIdAsync(
                    command.UserId,
                    cancellationToken);

        var now =
            _clock.UtcNow;

        foreach (var session in sessions)
        {
            if (session.Id == command.CurrentSessionId)
            {
                continue;
            }

            if (!session.IsRevoked)
            {
                session.Revoke(
                    now,
                    "UserRevokedOtherSessions");
            }

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
        }

        await _unitOfWork
            .SaveChangesAsync(
                cancellationToken);

        return Result.Success();
    }
}

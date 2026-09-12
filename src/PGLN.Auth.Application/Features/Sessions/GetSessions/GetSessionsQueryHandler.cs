using PGLN.Auth.Application.Abstractions.Messaging;
using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Application.Common;

namespace PGLN.Auth.Application.Features.Sessions.GetSessions;

public sealed class GetSessionsQueryHandler
    : ICommandHandler<GetSessionsQuery, Result<IReadOnlyCollection<SessionItem>>>
{
    private readonly IAuthSessionRepository _authSessionRepository;

    public GetSessionsQueryHandler(
        IAuthSessionRepository authSessionRepository)
    {
        _authSessionRepository = authSessionRepository;
    }

    public async Task<Result<IReadOnlyCollection<SessionItem>>> HandleAsync(
        GetSessionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var sessions =
            await _authSessionRepository
                .GetByUserIdAsync(
                    query.UserId,
                    cancellationToken);

        IReadOnlyCollection<SessionItem> items =
            sessions
                .OrderByDescending(session => session.LastSeenAtUtc)
                .Select(session =>
                    new SessionItem(
                        session.Id,
                        session.DeviceIdHash,
                        session.DeviceName,
                        session.IpAddress,
                        session.UserAgent,
                        session.CreatedAtUtc,
                        session.LastSeenAtUtc,
                        session.IsRevoked))
                .ToArray();

        return Result<IReadOnlyCollection<SessionItem>>.Success(items);
    }
}


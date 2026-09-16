using PGLN.Auth.Application.Abstractions.Persistence;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.TestDoubles;

public sealed class FakeAuthSessionRepository
    : IAuthSessionRepository
{
    private readonly List<AuthSession> _sessions = [];

    public IReadOnlyCollection<AuthSession> Sessions =>
        _sessions.AsReadOnly();

    public Task<AuthSession?> GetByIdAsync(
        AuthSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        var session =
            _sessions.SingleOrDefault(
                session =>
                    session.Id == sessionId);

        return Task.FromResult(
            session);
    }

    public Task<IReadOnlyCollection<AuthSession>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<AuthSession> sessions =
            _sessions
                .Where(
                    session =>
                        session.UserId == userId)
                .OrderByDescending(
                    session =>
                        session.LastSeenAtUtc)
                .ToArray();

        return Task.FromResult(
            sessions);
    }

    public Task<AuthSession?> GetActiveByDeviceIdHashAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        var session =
            _sessions.SingleOrDefault(
                session =>
                    session.UserId == userId &&
                    session.DeviceIdHash == deviceIdHash &&
                    !session.IsRevoked);

        return Task.FromResult(
            session);
    }

    public Task<bool> HasSeenDeviceAsync(
        UserId userId,
        string deviceIdHash,
        CancellationToken cancellationToken = default)
    {
        var hasSeenDevice =
            _sessions.Any(
                session =>
                    session.UserId == userId &&
                    session.DeviceIdHash == deviceIdHash);

        return Task.FromResult(
            hasSeenDevice);
    }

    public Task<bool> HasAnySessionAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var hasAnySession =
            _sessions.Any(
                session =>
                    session.UserId == userId);

        return Task.FromResult(
            hasAnySession);
    }

    public Task AddAsync(
        AuthSession session,
        CancellationToken cancellationToken = default)
    {
        _sessions.Add(
            session);

        return Task.CompletedTask;
    }

    public void Seed(
        AuthSession session)
    {
        _sessions.Add(
            session);
    }
}

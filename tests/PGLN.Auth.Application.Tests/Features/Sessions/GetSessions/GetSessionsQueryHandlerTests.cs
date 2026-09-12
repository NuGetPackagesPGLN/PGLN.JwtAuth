using PGLN.Auth.Application.Features.Sessions.GetSessions;
using PGLN.Auth.Application.Tests.TestDoubles;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Tests.Features.Sessions.GetSessions;

public sealed class GetSessionsQueryHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            11,
            8,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldReturnSessionsForUser()
    {
        var userId =
            UserId.New();

        var repository =
            new FakeAuthSessionRepository();

        var session =
            AuthSession.Create(
                AuthSessionId.New(),
                userId,
                "device-hash-001",
                "Chrome on Windows",
                "127.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-2));

        repository.Seed(
            session);

        var handler =
            new GetSessionsQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetSessionsQuery(
                    userId));

        Assert.True(
            result.IsSuccess);

        var item =
            Assert.Single(
                result.Value);

        Assert.Equal(
            session.Id,
            item.Id);

        Assert.Equal(
            session.DeviceIdHash,
            item.DeviceIdHash);

        Assert.Equal(
            session.DeviceName,
            item.DeviceName);

        Assert.Equal(
            session.IpAddress,
            item.IpAddress);

        Assert.Equal(
            session.UserAgent,
            item.UserAgent);

        Assert.Equal(
            session.CreatedAtUtc,
            item.CreatedAtUtc);

        Assert.Equal(
            session.LastSeenAtUtc,
            item.LastSeenAtUtc);

        Assert.False(
            item.IsRevoked);
    }

    [Fact]
    public async Task HandleAsync_ShouldOnlyReturnSessionsBelongingToRequestedUser()
    {
        var requestedUserId =
            UserId.New();

        var otherUserId =
            UserId.New();

        var repository =
            new FakeAuthSessionRepository();

        repository.Seed(
            AuthSession.Create(
                AuthSessionId.New(),
                requestedUserId,
                "requested-device",
                "Chrome",
                "127.0.0.1",
                "Chrome/1.0",
                Now.AddDays(-1)));

        repository.Seed(
            AuthSession.Create(
                AuthSessionId.New(),
                otherUserId,
                "other-device",
                "Safari",
                "127.0.0.2",
                "Safari/1.0",
                Now.AddDays(-1)));

        var handler =
            new GetSessionsQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetSessionsQuery(
                    requestedUserId));

        Assert.True(
            result.IsSuccess);

        var item =
            Assert.Single(
                result.Value);

        Assert.Equal(
            requestedUserId,
            repository.Sessions
                .Single(session => session.Id == item.Id)
                .UserId);
    }

    [Fact]
    public async Task HandleAsync_ShouldOrderSessionsByLastSeenDescending()
    {
        var userId =
            UserId.New();

        var repository =
            new FakeAuthSessionRepository();

        var oldest =
            AuthSession.Create(
                AuthSessionId.New(),
                userId,
                "device-oldest",
                "Old Device",
                "127.0.0.1",
                "Agent/1.0",
                Now.AddDays(-10));

        var middle =
            AuthSession.Create(
                AuthSessionId.New(),
                userId,
                "device-middle",
                "Middle Device",
                "127.0.0.2",
                "Agent/2.0",
                Now.AddDays(-5));

        var newest =
            AuthSession.Create(
                AuthSessionId.New(),
                userId,
                "device-newest",
                "Newest Device",
                "127.0.0.3",
                "Agent/3.0",
                Now.AddDays(-1));

        repository.Seed(oldest);
        repository.Seed(newest);
        repository.Seed(middle);

        var handler =
            new GetSessionsQueryHandler(
                repository);

        var result =
            await handler.HandleAsync(
                new GetSessionsQuery(
                    userId));

        Assert.True(
            result.IsSuccess);

        var items =
            result.Value.ToArray();

        Assert.Equal(
            3,
            items.Length);

        Assert.Equal(
            newest.Id,
            items[0].Id);

        Assert.Equal(
            middle.Id,
            items[1].Id);

        Assert.Equal(
            oldest.Id,
            items[2].Id);
    }}


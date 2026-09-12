using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class AuthSessionPersistenceTests
{
    [Fact]
    public async Task AuthSession_ShouldPersistAndReload()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(
                    connection)
                .Options;

        await using var dbContext =
            new AuthDbContext(
                options);

        await dbContext.Database
            .EnsureCreatedAsync();

        var now =
            DateTimeOffset.UtcNow;

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "session@example.com"),
                "hashed-password",
                now.AddDays(-1));

        user.ClearDomainEvents();

        var session =
            AuthSession.Create(
                AuthSessionId.New(),
                user.Id,
                "device-hash-123",
                "Chrome on Windows",
                "127.0.0.1",
                "Mozilla/5.0",
                now);

        dbContext.Users.Add(
            user);

        dbContext.AuthSessions.Add(
            session);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persistedSession =
            await dbContext.AuthSessions
                .SingleAsync(
                    item =>
                        item.Id ==
                        session.Id);

        Assert.Equal(
            session.Id,
            persistedSession.Id);

        Assert.Equal(
            user.Id,
            persistedSession.UserId);

        Assert.Equal(
            "device-hash-123",
            persistedSession.DeviceIdHash);

        Assert.Equal(
            "Chrome on Windows",
            persistedSession.DeviceName);

        Assert.Equal(
            "127.0.0.1",
            persistedSession.IpAddress);

        Assert.Equal(
            "Mozilla/5.0",
            persistedSession.UserAgent);

        Assert.Equal(
            now.ToUnixTimeMilliseconds(),
            persistedSession.CreatedAtUtc
                .ToUnixTimeMilliseconds());

        Assert.Equal(
            now.ToUnixTimeMilliseconds(),
            persistedSession.LastSeenAtUtc
                .ToUnixTimeMilliseconds());

        Assert.False(
            persistedSession.IsRevoked);

        Assert.True(
            persistedSession.IsActive);
    }
}

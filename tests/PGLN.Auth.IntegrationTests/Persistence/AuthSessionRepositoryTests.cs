using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.Sessions;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class AuthSessionRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnMatchingSession()
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
            CreateUser(
                now);

        var session =
            CreateSession(
                user.Id,
                "device-hash-1",
                now);

        dbContext.Users.Add(
            user);

        dbContext.AuthSessions.Add(
            session);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new AuthSessionRepository(
                dbContext);

        var result =
            await repository.GetByIdAsync(
                session.Id);

        Assert.NotNull(
            result);

        Assert.Equal(
            session.Id,
            result.Id);

        Assert.Equal(
            user.Id,
            result.UserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUsersSessions()
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
            CreateUser(
                now,
                "user@example.com");

        var otherUser =
            CreateUser(
                now,
                "other@example.com");

        var olderSession =
            CreateSession(
                user.Id,
                "device-hash-1",
                now.AddMinutes(-10));

        var newerSession =
            CreateSession(
                user.Id,
                "device-hash-2",
                now.AddMinutes(-5));

        var otherSession =
            CreateSession(
                otherUser.Id,
                "device-hash-3",
                now);

        dbContext.Users.AddRange(
            user,
            otherUser);

        dbContext.AuthSessions.AddRange(
            olderSession,
            newerSession,
            otherSession);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new AuthSessionRepository(
                dbContext);

        var result =
            await repository.GetByUserIdAsync(
                user.Id);

        Assert.Equal(
            2,
            result.Count);

        Assert.Equal(
            newerSession.Id,
            result.First().Id);

        Assert.DoesNotContain(
            result,
            session =>
                session.UserId ==
                otherUser.Id);
    }

    [Fact]
    public async Task GetActiveByDeviceIdHashAsync_ShouldReturnMatchingActiveSession()
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
            CreateUser(
                now);

        var activeSession =
            CreateSession(
                user.Id,
                "known-device",
                now);

        dbContext.Users.Add(
            user);

        dbContext.AuthSessions.Add(
            activeSession);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new AuthSessionRepository(
                dbContext);

        var result =
            await repository.GetActiveByDeviceIdHashAsync(
                user.Id,
                "known-device");

        Assert.NotNull(
            result);

        Assert.Equal(
            activeSession.Id,
            result.Id);
    }

    [Fact]
    public async Task GetActiveByDeviceIdHashAsync_WhenSessionRevoked_ShouldReturnNull()
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
            CreateUser(
                now);

        var session =
            CreateSession(
                user.Id,
                "revoked-device",
                now);

        session.Revoke(
            now.AddMinutes(1),
            "UserRequested");

        dbContext.Users.Add(
            user);

        dbContext.AuthSessions.Add(
            session);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new AuthSessionRepository(
                dbContext);

        var result =
            await repository.GetActiveByDeviceIdHashAsync(
                user.Id,
                "revoked-device");

        Assert.Null(
            result);
    }

    private static User CreateUser(
        DateTimeOffset now,
        string email = "session@example.com")
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    email),
                "hashed-password",
                now.AddDays(-1));

        user.ClearDomainEvents();

        return user;
    }

    private static AuthSession CreateSession(
        UserId userId,
        string deviceIdHash,
        DateTimeOffset createdAtUtc)
    {
        return AuthSession.Create(
            AuthSessionId.New(),
            userId,
            deviceIdHash,
            "Chrome on Windows",
            "127.0.0.1",
            "Mozilla/5.0",
            createdAtUtc);
    }
}

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class LoginAttemptPersistenceTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            7,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task SuccessfulLoginAttempt_ShouldPersist()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var context =
            new AuthDbContext(options);

        await context.Database
            .EnsureCreatedAsync();

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                Now.AddDays(-1));

        user.ClearDomainEvents();

        context.Users.Add(user);

        context.LoginAttempts.Add(
            LoginAttempt.Successful(
                "user@example.com",
                user.Id,
                Now));

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persisted =
            await context.LoginAttempts
                .SingleAsync();

        Assert.True(
            persisted.Succeeded);

        Assert.Equal(
            user.Id,
            persisted.UserId);

        Assert.Null(
            persisted.FailureReason);
    }

    [Fact]
    public async Task FailedAttemptForUnknownUser_ShouldPersistWithoutUserId()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var context =
            new AuthDbContext(options);

        await context.Database
            .EnsureCreatedAsync();

        context.LoginAttempts.Add(
            LoginAttempt.Failed(
                "missing@example.com",
                null,
                LoginFailureReason.InvalidCredentials,
                Now));

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persisted =
            await context.LoginAttempts
                .SingleAsync();

        Assert.False(
            persisted.Succeeded);

        Assert.Null(
            persisted.UserId);

        Assert.Equal(
            LoginFailureReason.InvalidCredentials,
            persisted.FailureReason);
    }
}

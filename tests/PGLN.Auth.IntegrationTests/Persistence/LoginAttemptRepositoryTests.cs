using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.LoginAttempts;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class LoginAttemptRepositoryTests
{
    [Fact]
    public async Task CountFailedAttemptsAsync_ShouldCountOnlyMatchingRecentFailures()
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

        var sinceUtc =
            now.AddMinutes(-5);

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                now.AddDays(-1));

        user.ClearDomainEvents();

        dbContext.Users.Add(
            user);

        dbContext.LoginAttempts.AddRange(
            LoginAttempt.Failed(
                "USER@EXAMPLE.COM",
                user.Id,
                LoginFailureReason.InvalidCredentials,
                now.AddMinutes(-1)),

            LoginAttempt.Failed(
                "USER@EXAMPLE.COM",
                user.Id,
                LoginFailureReason.InvalidCredentials,
                now.AddMinutes(-2)),

            LoginAttempt.Successful(
                "USER@EXAMPLE.COM",
                user.Id,
                now.AddMinutes(-1)),

            LoginAttempt.Failed(
                "USER@EXAMPLE.COM",
                user.Id,
                LoginFailureReason.InvalidCredentials,
                now.AddMinutes(-10)),

            LoginAttempt.Failed(
                "OTHER@EXAMPLE.COM",
                user.Id,
                LoginFailureReason.InvalidCredentials,
                now.AddMinutes(-1)));

        await dbContext.SaveChangesAsync();

        var repository =
            new LoginAttemptRepository(
                dbContext);

        var count =
            await repository.CountFailedAttemptsAsync(
                "USER@EXAMPLE.COM",
                sinceUtc);

        Assert.Equal(
            2,
            count);
    }
}

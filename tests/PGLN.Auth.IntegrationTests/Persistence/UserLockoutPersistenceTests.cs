using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class UserLockoutPersistenceTests
{
    [Fact]
    public async Task LockoutState_ShouldPersistAcrossDatabaseReload()
    {
        await using var application =
            await Http.HttpTestApplication.CreateAsync();

        var now =
            new DateTimeOffset(
                2026,
                9,
                10,
                16,
                0,
                0,
                TimeSpan.Zero);

        UserId userId;

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var user =
                User.Register(
                    UserId.New(),
                    Email.Create(
                        "lockout@example.com"),
                    "hashed-password",
                    now.AddDays(-1));

            user.ClearDomainEvents();

            user.RecordFailedLoginAttempt(
                now.AddMinutes(-2));

            user.RecordFailedLoginAttempt(
                now.AddMinutes(-1));

            user.LockOutUntil(
                now.AddMinutes(15));

            userId =
                user.Id;

            dbContext.Users.Add(
                user);

            await dbContext.SaveChangesAsync();
        }

        await using (
            var scope =
                application.Application.Services
                    .CreateAsyncScope())
        {
            var dbContext =
                scope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var storedUser =
                await dbContext.Users
                    .SingleAsync(
                        x => x.Id == userId);

            Assert.Equal(
                2,
                storedUser.FailedLoginAttempts);

            Assert.Equal(
                now.AddMinutes(-1),
                storedUser.LastFailedLoginAtUtc);

            Assert.Equal(
                now.AddMinutes(15),
                storedUser.LockoutEndUtc);

            Assert.True(
                storedUser.IsLockedOut(
                    now));
        }
    }
}

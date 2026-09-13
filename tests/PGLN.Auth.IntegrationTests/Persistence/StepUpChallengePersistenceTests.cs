using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class StepUpChallengePersistenceTests
{
    [Fact]
    public async Task StepUpChallenge_ShouldPersistAndReload()
    {
        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

        await using var dbContext =
            new AuthDbContext(
                options);

        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "stepup@example.com"),
                "password-hash",
                DateTimeOffset.UtcNow);

        dbContext.Users.Add(
            user);

        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                user.Id,
                "device-hash-001",
                "Chrome on Windows",
                "code-hash",
                now,
                now.AddMinutes(10));

        dbContext.StepUpChallenges.Add(
            challenge);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new StepUpChallengeRepository(
                dbContext);

        var reloaded =
            await repository.GetByIdAsync(
                challenge.Id);

        Assert.NotNull(
            reloaded);

        Assert.Equal(
            challenge.Id,
            reloaded!.Id);

        Assert.Equal(
            user.Id,
            reloaded.UserId);

        Assert.Equal(
            "device-hash-001",
            reloaded.DeviceIdHash);

        Assert.False(
            reloaded.IsVerified);

        Assert.Equal(
            0,
            reloaded.FailedAttempts);
    }

    [Fact]
    public async Task GetActiveByUserAndDeviceHashAsync_ShouldReturnActiveChallenge()
    {
        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite("Data Source=:memory:")
                .Options;

        await using var dbContext =
            new AuthDbContext(
                options);

        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "stepup@example.com"),
                "password-hash",
                DateTimeOffset.UtcNow);

        dbContext.Users.Add(
            user);

        var now =
            DateTimeOffset.UtcNow;

        var challenge =
            StepUpChallenge.Create(
                StepUpChallengeId.New(),
                user.Id,
                "device-hash-001",
                null,
                "code-hash",
                now,
                now.AddMinutes(10));

        dbContext.StepUpChallenges.Add(
            challenge);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new StepUpChallengeRepository(
                dbContext);

        var result =
            await repository
                .GetActiveByUserAndDeviceHashAsync(
                    user.Id,
                    "device-hash-001",
                    now);

        Assert.NotNull(
            result);

        Assert.Equal(
            challenge.Id,
            result!.Id);
    }
}




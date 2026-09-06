using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class UserRepositoryIntegrationTests
{
    [Fact]
    public async Task AddAndSaveAsync_ShouldPersistUser()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var repository =
            new UserRepository(dbContext);

        var unitOfWork =
            new UnitOfWork(dbContext);

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                new DateTimeOffset(
                    2026,
                    9,
                    6,
                    8,
                    0,
                    0,
                    TimeSpan.Zero));

        await repository.AddAsync(user);

        await unitOfWork.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persistedUser =
            await repository.GetByIdAsync(
                user.Id);

        Assert.NotNull(persistedUser);

        Assert.Equal(
            "user@example.com",
            persistedUser.Email.Value);

        Assert.Equal(
            "USER@EXAMPLE.COM",
            persistedUser.NormalizedEmail);

        Assert.Equal(
            "hashed-password",
            persistedUser.PasswordHash);

        Assert.False(
            persistedUser.EmailConfirmed);
    }

    [Fact]
    public async Task ExistsByNormalizedEmailAsync_WithDifferentCase_ShouldReturnTrue()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var repository =
            new UserRepository(dbContext);

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "User@Example.com"),
                "hashed-password",
                DateTimeOffset.UtcNow);

        await repository.AddAsync(user);

        await dbContext.SaveChangesAsync();

        var exists =
            await repository
                .ExistsByNormalizedEmailAsync(
                    "USER@EXAMPLE.COM");

        Assert.True(exists);
    }

    [Fact]
    public async Task NormalizedEmail_ShouldHaveUniqueConstraint()
    {
        await using var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<AuthDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var firstUser =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hash-1",
                DateTimeOffset.UtcNow);

        var secondUser =
            User.Register(
                UserId.New(),
                Email.Create(
                    "USER@example.com"),
                "hash-2",
                DateTimeOffset.UtcNow);

        dbContext.Users.Add(firstUser);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        dbContext.Users.Add(secondUser);

        await Assert.ThrowsAsync<
            DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }
}

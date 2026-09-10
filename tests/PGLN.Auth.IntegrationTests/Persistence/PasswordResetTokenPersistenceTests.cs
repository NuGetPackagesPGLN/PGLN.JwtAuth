using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.PasswordResets;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class PasswordResetTokenPersistenceTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            8,
            18,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task PasswordResetToken_ShouldPersistAndReload()
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
            new AuthDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        var user =
            CreateUser();

        var token =
            CreateToken(
                user.Id,
                "RESET-HASH-1");

        context.Users.Add(
            user);

        context.PasswordResetTokens.Add(
            token);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persisted =
            await context.PasswordResetTokens
                .SingleAsync();

        Assert.Equal(
            token.Id,
            persisted.Id);

        Assert.Equal(
            user.Id,
            persisted.UserId);

        Assert.Equal(
            "RESET-HASH-1",
            persisted.TokenHash);

        Assert.Equal(
            Now,
            persisted.CreatedAtUtc);

        Assert.Equal(
            Now.AddMinutes(30),
            persisted.ExpiresAtUtc);

        Assert.Null(
            persisted.UsedAtUtc);
    }

    [Fact]
    public async Task GetByTokenHashAsync_ShouldReturnMatchingToken()
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
            new AuthDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        var user =
            CreateUser();

        context.Users.Add(
            user);

        context.PasswordResetTokens.Add(
            CreateToken(
                user.Id,
                "MATCHING-HASH"));

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new PasswordResetTokenRepository(
                context);

        var persisted =
            await repository.GetByTokenHashAsync(
                "MATCHING-HASH");

        Assert.NotNull(
            persisted);

        Assert.Equal(
            "MATCHING-HASH",
            persisted.TokenHash);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ShouldReturnOnlyUsableTokens()
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
            new AuthDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        var user =
            CreateUser();

        context.Users.Add(
            user);

        var active =
            CreateToken(
                user.Id,
                "ACTIVE-HASH");

        var used =
            CreateToken(
                user.Id,
                "USED-HASH");

        used.MarkAsUsed(
            Now.AddMinutes(5));

        var expired =
            PasswordResetToken.Create(
                PasswordResetTokenId.New(),
                user.Id,
                "EXPIRED-HASH",
                Now.AddHours(-1),
                Now.AddMinutes(-1));

        context.PasswordResetTokens.AddRange(
            active,
            used,
            expired);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var repository =
            new PasswordResetTokenRepository(
                context);

        var results =
            await repository.GetActiveByUserIdAsync(
                user.Id,
                Now);

        var token =
            Assert.Single(
                results);

        Assert.Equal(
            active.Id,
            token.Id);
    }

    [Fact]
    public async Task TokenHash_ShouldHaveUniqueConstraint()
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
            new AuthDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        var user =
            CreateUser();

        context.Users.Add(
            user);

        context.PasswordResetTokens.Add(
            CreateToken(
                user.Id,
                "UNIQUE-HASH"));

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        context.PasswordResetTokens.Add(
            CreateToken(
                user.Id,
                "UNIQUE-HASH"));

        await Assert.ThrowsAsync<
            DbUpdateException>(
            () =>
                context.SaveChangesAsync());
    }

    [Fact]
    public async Task DeletingUser_ShouldDeletePasswordResetTokens()
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
            new AuthDbContext(
                options);

        await context.Database
            .EnsureCreatedAsync();

        var user =
            CreateUser();

        context.Users.Add(
            user);

        context.PasswordResetTokens.Add(
            CreateToken(
                user.Id,
                "DELETE-HASH"));

        await context.SaveChangesAsync();

        context.Users.Remove(
            user);

        await context.SaveChangesAsync();

        var count =
            await context.PasswordResetTokens
                .CountAsync();

        Assert.Equal(
            0,
            count);
    }

    private static User CreateUser()
    {
        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "password-reset@example.com"),
                "hashed-password",
                Now.AddDays(-1));

        user.ClearDomainEvents();

        return user;
    }

    private static PasswordResetToken CreateToken(
        UserId userId,
        string tokenHash)
    {
        return PasswordResetToken.Create(
            PasswordResetTokenId.New(),
            userId,
            tokenHash,
            Now,
            Now.AddMinutes(30));
    }
}

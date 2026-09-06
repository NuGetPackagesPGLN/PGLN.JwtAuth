using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class EmailVerificationTokenRepositoryIntegrationTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            6,
            14,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task AddAndSaveAsync_ShouldPersistToken()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var user =
            CreateUser();

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var repository =
            new EmailVerificationTokenRepository(
                dbContext);

        var token =
            CreateToken(
                user.Id,
                "HASH-ONE");

        await repository.AddAsync(token);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persisted =
            await repository.GetByTokenHashAsync(
                "HASH-ONE");

        Assert.NotNull(persisted);

        Assert.Equal(
            token.Id,
            persisted.Id);

        Assert.Equal(
            user.Id,
            persisted.UserId);

        Assert.Equal(
            "HASH-ONE",
            persisted.TokenHash);

        Assert.False(
            persisted.IsUsed);
    }

    [Fact]
    public async Task GetByTokenHashAsync_WithMatchingHash_ShouldReturnToken()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var user =
            CreateUser();

        dbContext.Users.Add(user);

        var token =
            CreateToken(
                user.Id,
                "LOOKUP-HASH");

        dbContext.EmailVerificationTokens.Add(
            token);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new EmailVerificationTokenRepository(
                dbContext);

        var result =
            await repository.GetByTokenHashAsync(
                "LOOKUP-HASH");

        Assert.NotNull(result);

        Assert.Equal(
            token.Id,
            result.Id);
    }

    [Fact]
    public async Task TokenHash_ShouldHaveUniqueConstraint()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var user =
            CreateUser();

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        dbContext.EmailVerificationTokens.Add(
            CreateToken(
                user.Id,
                "DUPLICATE-HASH"));

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        dbContext.EmailVerificationTokens.Add(
            CreateToken(
                user.Id,
                "DUPLICATE-HASH"));

        await Assert.ThrowsAsync<DbUpdateException>(
            () =>
                dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ShouldReturnUnusedTokens()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var user =
            CreateUser();

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        var activeToken =
            CreateToken(
                user.Id,
                "ACTIVE-HASH");

        var usedToken =
            CreateToken(
                user.Id,
                "USED-HASH");

        usedToken.MarkAsUsed(
            Now.AddMinutes(10));

        dbContext.EmailVerificationTokens.AddRange(
            activeToken,
            usedToken);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var repository =
            new EmailVerificationTokenRepository(
                dbContext);

        var results =
            await repository.GetActiveByUserIdAsync(
                user.Id);

        var token =
            Assert.Single(results);

        Assert.Equal(
            activeToken.Id,
            token.Id);
    }

    [Fact]
    public async Task DeletingUser_ShouldDeleteVerificationTokens()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var user =
            CreateUser();

        dbContext.Users.Add(user);

        dbContext.EmailVerificationTokens.Add(
            CreateToken(
                user.Id,
                "DELETE-HASH"));

        await dbContext.SaveChangesAsync();

        dbContext.Users.Remove(user);

        await dbContext.SaveChangesAsync();

        var count =
            await dbContext
                .EmailVerificationTokens
                .CountAsync();

        Assert.Equal(
            0,
            count);
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection =
            new SqliteConnection(
                "Data Source=:memory:");

        await connection.OpenAsync();

        return connection;
    }

    private static DbContextOptions<AuthDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static User CreateUser()
    {
        return User.Register(
            UserId.New(),
            Email.Create(
                "user@example.com"),
            "hashed-password",
            Now);
    }

    private static EmailVerificationToken CreateToken(
        UserId userId,
        string tokenHash)
    {
        return EmailVerificationToken.Create(
            EmailVerificationTokenId.New(),
            userId,
            tokenHash,
            Now,
            Now.AddHours(24));
    }
}

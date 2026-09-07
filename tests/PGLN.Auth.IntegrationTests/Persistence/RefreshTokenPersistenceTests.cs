using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Domain.RefreshTokens;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Persistence;

public sealed class RefreshTokenPersistenceTests
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
    public async Task RefreshToken_ShouldPersistAndReload()
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

        context.Users.Add(
            user);

        var token =
            RefreshToken.Create(
                RefreshTokenId.New(),
                user.Id,
                "HASHED-REFRESH-TOKEN",
                Now,
                Now.AddDays(30));

        context.RefreshTokens.Add(
            token);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persisted =
            await context.RefreshTokens
                .SingleAsync();

        Assert.Equal(
            user.Id,
            persisted.UserId);

        Assert.Equal(
            "HASHED-REFRESH-TOKEN",
            persisted.TokenHash);

        Assert.True(
            persisted.IsActive(Now));
    }

    [Fact]
    public async Task RotatedRefreshToken_ShouldPersistReplacementChain()
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

        var replacementId =
            RefreshTokenId.New();

        var token =
            RefreshToken.Create(
                RefreshTokenId.New(),
                user.Id,
                "HASHED-OLD-TOKEN",
                Now,
                Now.AddDays(30));

        token.Rotate(
            replacementId,
            Now.AddHours(1));

        context.RefreshTokens.Add(
            token);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var persisted =
            await context.RefreshTokens
                .SingleAsync();

        Assert.True(
            persisted.IsRevoked);

        Assert.Equal(
            "Rotated",
            persisted.RevocationReason);

        Assert.Equal(
            replacementId,
            persisted.ReplacedByTokenId);
    }
}

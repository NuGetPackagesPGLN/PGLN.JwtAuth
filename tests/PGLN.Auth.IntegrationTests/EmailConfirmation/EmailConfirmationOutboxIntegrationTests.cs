using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Features.EmailConfirmation;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Domain.VerificationTokens;
using PGLN.Auth.EntityFrameworkCore.Outbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;
using PGLN.Auth.IntegrationTests.Outbox;

namespace PGLN.Auth.IntegrationTests.EmailConfirmation;

public sealed class EmailConfirmationOutboxIntegrationTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            6,
            19,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task Confirmation_ShouldPersistUserTokenAndWelcomeOutboxMessageTogether()
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

        var user =
            User.Register(
                UserId.New(),
                Email.Create(
                    "user@example.com"),
                "hashed-password",
                Now.AddDays(-1));

        user.ClearDomainEvents();

        var token =
            EmailVerificationToken.Create(
                EmailVerificationTokenId.New(),
                user.Id,
                "HASHED-RAW-TOKEN",
                Now.AddHours(-1),
                Now.AddHours(23));

        dbContext.Users.Add(user);

        dbContext.EmailVerificationTokens.Add(
            token);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var userRepository =
            new UserRepository(
                dbContext);

        var tokenRepository =
            new EmailVerificationTokenRepository(
                dbContext);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                new TestPayloadProtector());

        var unitOfWork =
            new UnitOfWork(
                dbContext);

        var handler =
            new ConfirmEmailCommandHandler(
                tokenRepository,
                userRepository,
                new TestTokenHasher(),
                publisher,
                new TestClock(),
                unitOfWork);

        var result =
            await handler.HandleAsync(
                new ConfirmEmailCommand(
                    "raw-token"));

        Assert.True(
            result.IsSuccess);

        dbContext.ChangeTracker.Clear();

        var persistedUser =
            await dbContext
                .Users
                .SingleAsync();

        Assert.True(
            persistedUser.EmailConfirmed);

        Assert.Equal(
            Now,
            persistedUser.EmailConfirmedAtUtc);

        var persistedToken =
            await dbContext
                .EmailVerificationTokens
                .SingleAsync();

        Assert.True(
            persistedToken.IsUsed);

        Assert.Equal(
            Now,
            persistedToken.UsedAtUtc);

        var outbox =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.Contains(
            "WelcomeEmailRequested",
            outbox.Type,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "user@example.com",
            outbox.Payload,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(
            outbox.IsProcessed);
    }

    private sealed class TestTokenHasher
        : ITokenHasher
    {
        public string Hash(
            string token)
        {
            return "HASHED-RAW-TOKEN";
        }
    }

    private sealed class TestClock
        : IClock
    {
        public DateTimeOffset UtcNow =>
            Now;
    }
}

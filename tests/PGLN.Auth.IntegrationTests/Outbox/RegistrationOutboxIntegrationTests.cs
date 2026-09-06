using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Authentication;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Features.Registration;
using PGLN.Auth.EntityFrameworkCore.Outbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.Persistence.Repositories;

namespace PGLN.Auth.IntegrationTests.Outbox;

public sealed class RegistrationOutboxIntegrationTests
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
    public async Task Registration_ShouldPersistUserTokenAndProtectedOutboxMessageTogether()
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

        var userRepository =
            new UserRepository(dbContext);

        var tokenRepository =
            new EmailVerificationTokenRepository(
                dbContext);

        var outboxPublisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                new TestPayloadProtector());

        var unitOfWork =
            new UnitOfWork(dbContext);

        var handler =
            new RegisterCommandHandler(
                userRepository,
                tokenRepository,
                new TestPasswordHasher(),
                new TestTokenGenerator(),
                new TestTokenHasher(),
                outboxPublisher,
                unitOfWork,
                new TestClock(),
                new EmailVerificationOptions
                {
                    TokenLifetime =
                        TimeSpan.FromHours(24)
                });

        var result =
            await handler.HandleAsync(
                new RegisterCommand(
                    "user@example.com",
                    "SecretPassword123!"));

        Assert.True(
            result.IsSuccess);

        dbContext.ChangeTracker.Clear();

        Assert.Equal(
            1,
            await dbContext.Users.CountAsync());

        Assert.Equal(
            1,
            await dbContext
                .EmailVerificationTokens
                .CountAsync());

        Assert.Equal(
            1,
            await dbContext
                .OutboxMessages
                .CountAsync());

        var token =
            await dbContext
                .EmailVerificationTokens
                .SingleAsync();

        Assert.Equal(
            "HASHED-RAW-TOKEN",
            token.TokenHash);

        var outbox =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.DoesNotContain(
            "raw-token",
            outbox.Payload,
            StringComparison.OrdinalIgnoreCase);

        Assert.StartsWith(
            "PROTECTED::",
            outbox.Payload);
    }

    private sealed class TestPasswordHasher
        : IPasswordHasher
    {
        public string Hash(string password)
        {
            return "hashed-password";
        }

        public bool Verify(
            string password,
            string passwordHash)
        {
            return true;
        }
    }

    private sealed class TestTokenGenerator
        : IVerificationTokenGenerator
    {
        public string Generate()
        {
            return "raw-token";
        }
    }

    private sealed class TestTokenHasher
        : ITokenHasher
    {
        public string Hash(string token)
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

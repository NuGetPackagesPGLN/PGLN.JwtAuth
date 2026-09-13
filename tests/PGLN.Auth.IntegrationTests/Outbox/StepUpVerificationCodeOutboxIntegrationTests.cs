using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Domain.StepUpChallenges;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Outbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Outbox;

public sealed class StepUpVerificationCodeOutboxIntegrationTests
{
    [Fact]
    public async Task PublishAsync_ShouldPersistProtectedStepUpVerificationCode()
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

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                new TestPayloadProtector());

        var occurredAtUtc =
            new DateTimeOffset(
                2026,
                9,
                13,
                8,
                0,
                0,
                TimeSpan.Zero);

        var integrationEvent =
            new StepUpVerificationCodeRequested(
                Guid.NewGuid(),
                new UserId(
                    Guid.NewGuid()),
                new StepUpChallengeId(
                    Guid.NewGuid()),
                "user@example.com",
                "123456",
                "Developer Laptop",
                "192.0.2.10",
                "Test Browser",
                occurredAtUtc.AddMinutes(10),
                occurredAtUtc);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var outboxMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.DoesNotContain(
            "123456",
            outboxMessage.Payload,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "user@example.com",
            outboxMessage.Payload,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "\"code\"",
            outboxMessage.Payload,
            StringComparison.OrdinalIgnoreCase);

        Assert.StartsWith(
            "PROTECTED::",
            outboxMessage.Payload);

        Assert.Contains(
            nameof(StepUpVerificationCodeRequested),
            outboxMessage.Type,
            StringComparison.Ordinal);
    }
}

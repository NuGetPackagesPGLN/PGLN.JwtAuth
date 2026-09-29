using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.EntityFrameworkCore.Inbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Inbox;

public sealed class EfIntegrationEventInboxTests
{
    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            28,
            12,
            0,
            0,
            TimeSpan.Zero);

    private static readonly TimeSpan ClaimDuration =
        TimeSpan.FromMinutes(2);

    [Fact]
    public async Task TryClaimAsync_NewMessage_ShouldClaimMessage()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "727926b8-ea60-4d65-bb48-f052b67fcceb");

        var result =
            await inbox.TryClaimAsync(
                messageId,
                "worker-a",
                Now,
                ClaimDuration);

        Assert.Equal(
            IntegrationEventInboxClaimResult.Claimed,
            result);

        var message =
            await dbContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            messageId,
            message.Id);

        Assert.Equal(
            "worker-a",
            message.ClaimedBy);

        Assert.Equal(
            Now,
            message.ClaimedAtUtc);

        Assert.Equal(
            Now.Add(ClaimDuration),
            message.ClaimExpiresAtUtc);

        Assert.False(
            message.IsProcessed);
    }

    [Fact]
    public async Task TryClaimAsync_ActiveClaim_ShouldReturnAlreadyClaimed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "419848f9-c1df-4b4e-830e-dfa5b2754bdb");

        var first =
            await inbox.TryClaimAsync(
                messageId,
                "worker-a",
                Now,
                ClaimDuration);

        var second =
            await inbox.TryClaimAsync(
                messageId,
                "worker-b",
                Now.AddSeconds(30),
                ClaimDuration);

        Assert.Equal(
            IntegrationEventInboxClaimResult.Claimed,
            first);

        Assert.Equal(
            IntegrationEventInboxClaimResult.AlreadyClaimed,
            second);

        var message =
            await dbContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "worker-a",
            message.ClaimedBy);
    }

    [Fact]
    public async Task TryClaimAsync_ExpiredClaim_ShouldAllowTakeover()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "21d5538a-7595-4e22-97d1-07f967e6a568");

        await inbox.TryClaimAsync(
            messageId,
            "worker-a",
            Now,
            ClaimDuration);

        var takeoverAtUtc =
            Now.Add(ClaimDuration);

        var result =
            await inbox.TryClaimAsync(
                messageId,
                "worker-b",
                takeoverAtUtc,
                ClaimDuration);

        Assert.Equal(
            IntegrationEventInboxClaimResult.Claimed,
            result);

        var message =
            await dbContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "worker-b",
            message.ClaimedBy);

        Assert.Equal(
            takeoverAtUtc,
            message.ClaimedAtUtc);
    }

    [Fact]
    public async Task MarkProcessedAsync_ShouldMakeFutureDeliveryAlreadyProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "baae04dd-f7d2-40b5-a6a7-44978d4c51df");

        await inbox.TryClaimAsync(
            messageId,
            "worker-a",
            Now,
            ClaimDuration);

        var processedAtUtc =
            Now.AddMinutes(1);

        await inbox.MarkProcessedAsync(
            messageId,
            "worker-a",
            processedAtUtc);

        var result =
            await inbox.TryClaimAsync(
                messageId,
                "worker-b",
                Now.AddHours(1),
                ClaimDuration);

        Assert.Equal(
            IntegrationEventInboxClaimResult.AlreadyProcessed,
            result);

        var message =
            await dbContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.True(
            message.IsProcessed);

        Assert.Equal(
            processedAtUtc,
            message.ProcessedAtUtc);

        Assert.Null(
            message.ClaimedBy);

        Assert.Null(
            message.ClaimedAtUtc);

        Assert.Null(
            message.ClaimExpiresAtUtc);
    }

    [Fact]
    public async Task ReleaseAsync_ShouldAllowAnotherWorkerToClaim()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "951881ed-96e8-4f66-8284-a106702e8cf9");

        await inbox.TryClaimAsync(
            messageId,
            "worker-a",
            Now,
            ClaimDuration);

        await inbox.ReleaseAsync(
            messageId,
            "worker-a");

        var result =
            await inbox.TryClaimAsync(
                messageId,
                "worker-b",
                Now.AddSeconds(30),
                ClaimDuration);

        Assert.Equal(
            IntegrationEventInboxClaimResult.Claimed,
            result);

        var message =
            await dbContext
                .InboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            "worker-b",
            message.ClaimedBy);
    }

    [Fact]
    public async Task MarkProcessedAsync_NonOwningWorker_ShouldThrow()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "3e3c69a7-f5c8-49a2-a729-63f83eb60647");

        await inbox.TryClaimAsync(
            messageId,
            "worker-a",
            Now,
            ClaimDuration);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    inbox.MarkProcessedAsync(
                        messageId,
                        "worker-b",
                        Now.AddMinutes(1)));

        Assert.Equal(
            "Inbox message is not claimed by this worker.",
            exception.Message);
    }

    [Fact]
    public async Task ReleaseAsync_NonOwningWorker_ShouldThrow()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var inbox =
            new EfIntegrationEventInbox(
                dbContext);

        var messageId =
            Guid.Parse(
                "c6c9e05d-70e5-4655-a52c-0b33ef41a273");

        await inbox.TryClaimAsync(
            messageId,
            "worker-a",
            Now,
            ClaimDuration);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () =>
                    inbox.ReleaseAsync(
                        messageId,
                        "worker-b"));

        Assert.Equal(
            "Inbox message is not claimed by this worker.",
            exception.Message);
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
}

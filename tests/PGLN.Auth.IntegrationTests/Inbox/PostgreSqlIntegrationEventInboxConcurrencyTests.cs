using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;
using PGLN.Auth.EntityFrameworkCore.PostgreSql;

namespace PGLN.Auth.IntegrationTests.Inbox;

public sealed class PostgreSqlIntegrationEventInboxConcurrencyTests
{
    private static readonly TimeSpan ClaimDuration =
        TimeSpan.FromMinutes(2);

    [Fact]
    public async Task TryClaimAsync_ConcurrentNewMessage_ShouldHaveSingleOwner()
    {
        var connectionString =
            Environment.GetEnvironmentVariable(
                "PGLN_AUTH_POSTGRES_TEST_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "PGLN_AUTH_POSTGRES_TEST_CONNECTION_STRING is required.");
        }

        var services =
            new ServiceCollection();

        services.AddPGLNAuthPostgreSqlIntegrationEventInbox(
            connectionString);

        await using var provider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true,
                    ValidateOnBuild = true
                });

        var messageId =
            Guid.NewGuid();

        var now =
            DateTimeOffset.UtcNow;

        try
        {
            var ready =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);

            var workerATask =
                ClaimAsync(
                    provider,
                    messageId,
                    "postgres-worker-a",
                    now,
                    ready.Task);

            var workerBTask =
                ClaimAsync(
                    provider,
                    messageId,
                    "postgres-worker-b",
                    now,
                    ready.Task);

            ready.SetResult(true);

            var results =
                await Task.WhenAll(
                    workerATask,
                    workerBTask);

            Assert.Equal(
                1,
                results.Count(
                    result =>
                        result ==
                        IntegrationEventInboxClaimResult.Claimed));

            Assert.Equal(
                1,
                results.Count(
                    result =>
                        result ==
                        IntegrationEventInboxClaimResult.AlreadyClaimed));

            Assert.DoesNotContain(
                IntegrationEventInboxClaimResult.AlreadyProcessed,
                results);

            await using var verificationScope =
                provider.CreateAsyncScope();

            var dbContext =
                verificationScope.ServiceProvider
                    .GetRequiredService<AuthDbContext>();

            var persistedMessage =
                await dbContext
                    .InboxMessages
                    .AsNoTracking()
                    .SingleAsync(
                        message =>
                            message.Id == messageId);

            Assert.Contains(
                persistedMessage.ClaimedBy,
                new[]
                {
                    "postgres-worker-a",
                    "postgres-worker-b"
                });

            Assert.NotNull(
                persistedMessage.ClaimedAtUtc);

            Assert.NotNull(
                persistedMessage.ClaimExpiresAtUtc);

            Assert.Null(
                persistedMessage.ProcessedAtUtc);
        }
        finally
        {
            await DeleteTestMessageAsync(
                provider,
                messageId);
        }
    }

    private static async Task<IntegrationEventInboxClaimResult> ClaimAsync(
        IServiceProvider provider,
        Guid messageId,
        string workerId,
        DateTimeOffset utcNow,
        Task startSignal)
    {
        await using var scope =
            provider.CreateAsyncScope();

        var inbox =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventInbox>();

        await startSignal;

        return await inbox.TryClaimAsync(
            messageId,
            workerId,
            utcNow,
            ClaimDuration);
    }

    private static async Task DeleteTestMessageAsync(
        IServiceProvider provider,
        Guid messageId)
    {
        await using var scope =
            provider.CreateAsyncScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<AuthDbContext>();

        await dbContext
            .InboxMessages
            .Where(
                message =>
                    message.Id == messageId)
            .ExecuteDeleteAsync();
    }
}
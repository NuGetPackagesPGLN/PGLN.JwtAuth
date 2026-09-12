using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Email;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Time;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Application.Events.Email.Handlers;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.EntityFrameworkCore.Outbox;
using PGLN.Auth.EntityFrameworkCore.Persistence;

namespace PGLN.Auth.IntegrationTests.Outbox;

public sealed class OutboxProcessorIntegrationTests
{
    private static readonly OutboxProcessingOptions ProcessingOptions =
        new()
        {
            BatchSize = 20,
            MaximumAttempts = 5,
            InitialRetryDelay =
                TimeSpan.FromSeconds(10),
            MaximumRetryDelay =
                TimeSpan.FromMinutes(5),
            ClaimDuration =
                TimeSpan.FromMinutes(2)
        };

    private static readonly DateTimeOffset Now =
        new(
            2026,
            9,
            6,
            16,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_WithConfirmationEvent_ShouldSendEmailAndMarkMessageProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        var integrationEvent =
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw-token",
                Now);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync("integration-test-worker");

        Assert.Equal(
            1,
            processed);

        var message =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            message.IsProcessed);

        Assert.Equal(
            Now,
            message.ProcessedAtUtc);

        Assert.Equal(
            1,
            message.AttemptCount);

        Assert.Null(
            message.LastError);

        var email =
            Assert.Single(
                emailSender.Messages);

        Assert.Equal(
            "user@example.com",
            email.To);

        Assert.Contains(
            "token=raw-token",
            email.HtmlBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessAsync_WithPasswordChangedNotification_ShouldSendEmailAndMarkMessageProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        var integrationEvent =
            new PasswordChangedNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                Now);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            1,
            processed);

        var message =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            message.IsProcessed);

        Assert.Equal(
            Now,
            message.ProcessedAtUtc);

        Assert.Equal(
            1,
            message.AttemptCount);

        Assert.Null(
            message.LastError);

        var email =
            Assert.Single(
                emailSender.Messages);

        Assert.Equal(
            "user@example.com",
            email.To);

        Assert.Equal(
            "Your password was changed",
            email.Subject);

        Assert.Contains(
            Now.ToString("O"),
            email.HtmlBody,
            StringComparison.Ordinal);
    }
    [Fact]
    public async Task ProcessAsync_WithNewDeviceLoginNotification_ShouldSendEmailAndMarkMessageProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        var integrationEvent =
            new NewDeviceLoginNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "device-hash-001",
                "Chrome on Windows",
                "192.168.1.25",
                "Mozilla/5.0",
                Now);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            1,
            processed);

        var message =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            message.IsProcessed);

        Assert.Equal(
            Now,
            message.ProcessedAtUtc);

        Assert.Equal(
            1,
            message.AttemptCount);

        Assert.Null(
            message.LastError);

        var email =
            Assert.Single(
                emailSender.Messages);

        Assert.Equal(
            "user@example.com",
            email.To);

        Assert.Equal(
            "New device login detected",
            email.Subject);

        Assert.Contains(
            "Chrome on Windows",
            email.HtmlBody,
            StringComparison.Ordinal);

        Assert.Contains(
            "192.168.1.25",
            email.HtmlBody,
            StringComparison.Ordinal);

        Assert.Contains(
            "Mozilla/5.0",
            email.HtmlBody,
            StringComparison.Ordinal);

        Assert.Contains(
            Now.ToString("O"),
            email.HtmlBody,
            StringComparison.Ordinal);
    }
    [Fact]
    public async Task ProcessAsync_WhenHandlerFails_ShouldRecordFailureAndLeaveMessageUnprocessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender
            {
                ThrowOnSend = true
            };

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        await publisher.PublishAsync(
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw-token",
                Now));

        await dbContext.SaveChangesAsync();

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync("integration-test-worker");

        Assert.Equal(
            0,
            processed);

        var message =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.False(
            message.IsProcessed);

        Assert.Equal(
            1,
            message.AttemptCount);

        Assert.Equal(
            "Simulated email provider failure.",
            message.LastError);
    }

    [Fact]
    public async Task ProcessAsync_AfterPreviousFailure_ShouldRetryMessage()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender
            {
                ThrowOnSend = true
            };

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        await publisher.PublishAsync(
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw-token",
                Now));

        await dbContext.SaveChangesAsync();

        var clock =
            new TestClock(Now);

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                clock,
                ProcessingOptions);

        // ====================================================
        // First attempt fails
        // ====================================================

        var firstAttempt =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            0,
            firstAttempt);

        var failedMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.False(
            failedMessage.IsProcessed);

        Assert.Equal(
            1,
            failedMessage.AttemptCount);

        Assert.Equal(
            Now.AddSeconds(10),
            failedMessage.NextAttemptAtUtc);

        // ====================================================
        // Immediate retry must NOT happen
        // ====================================================

        emailSender.ThrowOnSend =
            false;

        var immediateRetry =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            0,
            immediateRetry);

        Assert.Empty(
            emailSender.Messages);

        // ====================================================
        // Move time to the retry boundary
        // ====================================================

        clock.Advance(
            ProcessingOptions.InitialRetryDelay);

        // ====================================================
        // Retry should now succeed
        // ====================================================

        var secondAttempt =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            1,
            secondAttempt);

        var processedMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            processedMessage.IsProcessed);

        Assert.Equal(
            2,
            processedMessage.AttemptCount);

        Assert.Null(
            processedMessage.LastError);

        Assert.Null(
            processedMessage.NextAttemptAtUtc);

        Assert.Single(
            emailSender.Messages);
    }

    [Fact]
    public async Task ProcessAsync_ShouldNotProcessAlreadyProcessedMessageAgain()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        await publisher.PublishAsync(
            new EmailConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                "raw-token",
                Now));

        await dbContext.SaveChangesAsync();

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var first =
            await processor.ProcessAsync("integration-test-worker");

        var second =
            await processor.ProcessAsync("integration-test-worker");

        Assert.Equal(
            1,
            first);

        Assert.Equal(
            0,
            second);

        Assert.Single(
            emailSender.Messages);
    }

    [Fact]
    public async Task ProcessAsync_WithAccountLockedNotification_ShouldSendEmailAndMarkMessageProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        var lockedUntilUtc =
            Now.AddMinutes(15);

        var integrationEvent =
            new AccountLockedNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "user@example.com",
                lockedUntilUtc,
                Now);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            1,
            processed);

        var message =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            message.IsProcessed);

        Assert.Equal(
            Now,
            message.ProcessedAtUtc);

        Assert.Equal(
            1,
            message.AttemptCount);

        Assert.Null(
            message.LastError);

        var email =
            Assert.Single(
                emailSender.Messages);

        Assert.Equal(
            "user@example.com",
            email.To);

        Assert.Equal(
            "Your account has been temporarily locked",
            email.Subject);

        Assert.Contains(
            Now.ToString("O"),
            email.HtmlBody,
            StringComparison.Ordinal);

        Assert.Contains(
            lockedUntilUtc.ToString("O"),
            email.HtmlBody,
            StringComparison.Ordinal);
    }
    [Fact]
    public async Task ProcessAsync_WithEmailChangeConfirmationEvent_ShouldSendEmailAndMarkMessageProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        var integrationEvent =
            new EmailChangeConfirmationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "new-email@example.com",
                "email-change-token",
                Now);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        var storedMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.False(
            storedMessage.IsProcessed);

        Assert.DoesNotContain(
            "email-change-token",
            storedMessage.Payload,
            StringComparison.Ordinal);

        Assert.StartsWith(
            "PROTECTED::",
            storedMessage.Payload);

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            1,
            processed);

        var processedMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            processedMessage.IsProcessed);

        Assert.Equal(
            Now,
            processedMessage.ProcessedAtUtc);

        Assert.Equal(
            1,
            processedMessage.AttemptCount);

        Assert.Null(
            processedMessage.LastError);

        var email =
            Assert.Single(
                emailSender.Messages);

        Assert.Equal(
            "new-email@example.com",
            email.To);

        Assert.Equal(
            "Confirm your new email",
            email.Subject);

        Assert.Contains(
            "https://app.example.com/confirm-email-change",
            email.HtmlBody,
            StringComparison.Ordinal);

        Assert.Contains(
            "email-change-token",
            email.HtmlBody,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProcessAsync_WithEmailChangedNotification_ShouldSendEmailToOldAddressAndMarkMessageProcessed()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        var options =
            CreateOptions(connection);

        await using var dbContext =
            new AuthDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var protector =
            new TestPayloadProtector();

        var emailSender =
            new TestEmailSender();

        using var serviceProvider =
            CreateServiceProvider(
                emailSender);

        var dispatcher =
            new IntegrationEventDispatcher(
                serviceProvider);

        var publisher =
            new OutboxIntegrationEventPublisher(
                dbContext,
                protector);

        var integrationEvent =
            new EmailChangedNotificationRequested(
                Guid.NewGuid(),
                UserId.New(),
                "old@example.com",
                "new@example.com",
                Now);

        await publisher.PublishAsync(
            integrationEvent);

        await dbContext.SaveChangesAsync();

        var storedMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.False(
            storedMessage.IsProcessed);

        var processor =
            new OutboxProcessor(
                dbContext,
                protector,
                new IntegrationEventTypeRegistry(),
                dispatcher,
                new TestClock(Now),
                ProcessingOptions);

        var processed =
            await processor.ProcessAsync(
                "integration-test-worker");

        Assert.Equal(
            1,
            processed);

        var processedMessage =
            await dbContext
                .OutboxMessages
                .SingleAsync();

        Assert.True(
            processedMessage.IsProcessed);

        Assert.Equal(
            Now,
            processedMessage.ProcessedAtUtc);

        Assert.Equal(
            1,
            processedMessage.AttemptCount);

        Assert.Null(
            processedMessage.LastError);

        var email =
            Assert.Single(
                emailSender.Messages);

        Assert.Equal(
            "old@example.com",
            email.To);

        Assert.Equal(
            "Your email address was changed",
            email.Subject);

        Assert.Contains(
            "old@example.com",
            email.HtmlBody,
            StringComparison.Ordinal);

        Assert.Contains(
            "new@example.com",
            email.HtmlBody,
            StringComparison.Ordinal);
    }
    private static ServiceProvider CreateServiceProvider(
        TestEmailSender sender)
    {
        var services =
            new ServiceCollection();

        services.AddSingleton<
            IEmailSender>(
            sender);

        services.AddSingleton<
            IEmailTemplateRenderer,
            TestEmailTemplateRenderer>();

        services.AddSingleton(
            new EmailDeliveryOptions
            {
                ConfirmationBaseUrl =
                    "https://app.example.com/confirm-email",

                EmailChangeConfirmationBaseUrl =
                    "https://app.example.com/confirm-email-change"
            });

        services.AddTransient<
            IIntegrationEventHandler<EmailConfirmationRequested>,
            EmailConfirmationRequestedHandler>();

        services.AddTransient<
            IIntegrationEventHandler<EmailChangeConfirmationRequested>,
            EmailChangeConfirmationRequestedHandler>();

        services.AddTransient<
            IIntegrationEventHandler<EmailChangedNotificationRequested>,
            EmailChangedNotificationRequestedHandler>();

        services.AddTransient<
            IIntegrationEventHandler<WelcomeEmailRequested>,
            WelcomeEmailRequestedHandler>();

        services.AddTransient<
            IIntegrationEventHandler<PasswordChangedNotificationRequested>,
            PasswordChangedNotificationRequestedHandler>();

        services.AddTransient<
            IIntegrationEventHandler<NewDeviceLoginNotificationRequested>,
            NewDeviceLoginNotificationRequestedHandler>();

        services.AddTransient<
            IIntegrationEventHandler<AccountLockedNotificationRequested>,
            AccountLockedNotificationRequestedHandler>();

        return services.BuildServiceProvider();
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

    private sealed class TestClock
        : IClock
    {
        public TestClock(
            DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; private set; }

        public void Advance(
            TimeSpan duration)
        {
            UtcNow =
                UtcNow.Add(duration);
        }
    }
}
















using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.Application.Events.Email;
using PGLN.Auth.Aws.Events;
using PGLN.Auth.Domain.Users;
using PGLN.Auth.Sample.AwsEmailWorker;

namespace PGLN.Auth.Aws.Tests.Worker;

public sealed class AwsEmailWorkerFunctionTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task FunctionHandler_MixedBatch_ShouldReturnOnlyFailedMessage()
    {
        // Arrange
        var dispatcher =
            new RecordingIntegrationEventDispatcher();

        var inbox =
            new RecordingIntegrationEventInbox();

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        services.AddSingleton<
            IIntegrationEventInbox>(
                inbox);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var validEvent =
            CreateEmailConfirmationRequested();

        var durableMessageId =
            Guid.Parse(
                "9d659805-8256-44e0-88d0-403ee8c94ed5");

        var validRecord =
            CreateRecord(
                "valid-message",
                CreateEnvelopeBody(
                    typeof(EmailConfirmationRequested).FullName!,
                    validEvent,
                    messageId:
                        durableMessageId));

        var invalidRecord =
            CreateRecord(
                "invalid-message",
                CreateEnvelopeBody(
                    "Untrusted.Namespace.ArbitraryEvent",
                    new
                    {
                        value = "untrusted"
                    }));

        var sqsEvent =
            new SQSEvent
            {
                Records =
                [
                    validRecord,
                    invalidRecord
                ]
            };

        var context =
            new TestLambdaContext();

        // Act
        var response =
            await function.FunctionHandler(
                sqsEvent,
                context);

        // Assert
        Assert.Single(
            dispatcher.DispatchedEvents);

        Assert.IsType<EmailConfirmationRequested>(
            dispatcher.DispatchedEvents[0]);

        Assert.Equal(
            [durableMessageId],
            inbox.ClaimedMessageIds);

        Assert.Equal(
            [durableMessageId],
            inbox.ProcessedMessageIds);

        Assert.Empty(
            inbox.ReleasedMessageIds);

        var failure =
            Assert.Single(
                response.BatchItemFailures);

        Assert.Equal(
            "invalid-message",
            failure.ItemIdentifier);
    }

    [Fact]
    public async Task FunctionHandler_AlreadyProcessed_ShouldAcknowledgeWithoutDispatching()
    {
        // Arrange
        var dispatcher =
            new RecordingIntegrationEventDispatcher();

        var inbox =
            new RecordingIntegrationEventInbox
            {
                ClaimResult =
                    IntegrationEventInboxClaimResult.AlreadyProcessed
            };

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        services.AddSingleton<
            IIntegrationEventInbox>(
                inbox);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var durableMessageId =
            Guid.Parse(
                "4b4f5a79-8d80-4b12-9624-98d55b09db26");

        var integrationEvent =
            CreateEmailConfirmationRequested();

        var record =
            CreateRecord(
                "already-processed-message",
                CreateEnvelopeBody(
                    typeof(EmailConfirmationRequested).FullName!,
                    integrationEvent,
                    messageId:
                        durableMessageId));

        var sqsEvent =
            new SQSEvent
            {
                Records =
                [
                    record
                ]
            };

        var context =
            new TestLambdaContext();

        // Act
        var response =
            await function.FunctionHandler(
                sqsEvent,
                context);

        // Assert
        Assert.Equal(
            [durableMessageId],
            inbox.ClaimedMessageIds);

        Assert.Empty(
            dispatcher.DispatchedEvents);

        Assert.Empty(
            dispatcher.DispatchContexts);

        Assert.Empty(
            inbox.ProcessedMessageIds);

        Assert.Empty(
            inbox.ReleasedMessageIds);

        Assert.Empty(
            response.BatchItemFailures);
    }
    [Fact]
    public async Task FunctionHandler_AlreadyClaimed_ShouldReturnMessageAsFailedWithoutDispatching()
    {
        // Arrange
        var dispatcher =
            new RecordingIntegrationEventDispatcher();

        var inbox =
            new RecordingIntegrationEventInbox
            {
                ClaimResult =
                    IntegrationEventInboxClaimResult.AlreadyClaimed
            };

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        services.AddSingleton<
            IIntegrationEventInbox>(
                inbox);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var durableMessageId =
            Guid.Parse(
                "1b70b837-b8db-470f-b690-305f24ed6746");

        var integrationEvent =
            CreateEmailConfirmationRequested();

        var record =
            CreateRecord(
                "already-claimed-message",
                CreateEnvelopeBody(
                    typeof(EmailConfirmationRequested).FullName!,
                    integrationEvent,
                    messageId:
                        durableMessageId));

        var sqsEvent =
            new SQSEvent
            {
                Records =
                [
                    record
                ]
            };

        var context =
            new TestLambdaContext();

        // Act
        var response =
            await function.FunctionHandler(
                sqsEvent,
                context);

        // Assert
        Assert.Equal(
            [durableMessageId],
            inbox.ClaimedMessageIds);

        Assert.Empty(
            dispatcher.DispatchedEvents);

        Assert.Empty(
            dispatcher.DispatchContexts);

        Assert.Empty(
            inbox.ProcessedMessageIds);

        Assert.Empty(
            inbox.ReleasedMessageIds);

        var failure =
            Assert.Single(
                response.BatchItemFailures);

        Assert.Equal(
            "already-claimed-message",
            failure.ItemIdentifier);
    }
    [Fact]
    public async Task FunctionHandler_MalformedJson_ShouldReturnMessageAsFailed()
    {
        // Arrange
        var dispatcher =
            new RecordingIntegrationEventDispatcher();

        var inbox =
            new RecordingIntegrationEventInbox();

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        services.AddSingleton<
            IIntegrationEventInbox>(
                inbox);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var sqsEvent =
            new SQSEvent
            {
                Records =
                [
                    CreateRecord(
                        "malformed-message",
                        "{ this-is-not-valid-json")
                ]
            };

        var context =
            new TestLambdaContext();

        // Act
        var response =
            await function.FunctionHandler(
                sqsEvent,
                context);

        // Assert
        Assert.Empty(
            dispatcher.DispatchedEvents);

        var failure =
            Assert.Single(
                response.BatchItemFailures);

        Assert.Equal(
            "malformed-message",
            failure.ItemIdentifier);
    }
    [Fact]
    public async Task FunctionHandler_DispatcherFailure_ShouldReleaseClaimAndReturnMessageAsFailed()
    {
        // Arrange
        var dispatcher =
            new FailingIntegrationEventDispatcher();

        var inbox =
            new RecordingIntegrationEventInbox();

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        services.AddSingleton<
            IIntegrationEventInbox>(
                inbox);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var durableMessageId =
            Guid.Parse(
                "aa1797df-475f-40dd-971f-b6d19ea93b4f");

        var integrationEvent =
            CreateEmailConfirmationRequested();

        var record =
            CreateRecord(
                "dispatcher-failure-message",
                CreateEnvelopeBody(
                    typeof(EmailConfirmationRequested).FullName!,
                    integrationEvent,
                    messageId:
                        durableMessageId));

        var sqsEvent =
            new SQSEvent
            {
                Records =
                [
                    record
                ]
            };

        var context =
            new TestLambdaContext();

        // Act
        var response =
            await function.FunctionHandler(
                sqsEvent,
                context);

        // Assert
        Assert.Equal(
            [durableMessageId],
            inbox.ClaimedMessageIds);

        Assert.Equal(
            1,
            dispatcher.AttemptCount);

        Assert.Empty(
            inbox.ProcessedMessageIds);

        Assert.Equal(
            [durableMessageId],
            inbox.ReleasedMessageIds);

        var failure =
            Assert.Single(
                response.BatchItemFailures);

        Assert.Equal(
            "dispatcher-failure-message",
            failure.ItemIdentifier);
    }

    private static EmailConfirmationRequested
        CreateEmailConfirmationRequested()
    {
        return new EmailConfirmationRequested(
            EventId:
                Guid.Parse(
                    "17bb1e4f-1235-48ea-b26a-fc5941142891"),
            UserId:
                new UserId(
                    Guid.Parse(
                        "45a7cb1a-d035-4824-a3ea-f286805de51e")),
            Email:
                "worker.test@example.com",
            VerificationToken:
                "synthetic-test-token",
            OccurredAtUtc:
                new DateTimeOffset(
                    2026,
                    9,
                    27,
                    10,
                    30,
                    0,
                    TimeSpan.Zero));
    }

    private static SQSEvent.SQSMessage CreateRecord(
        string messageId,
        string body)
    {
        return new SQSEvent.SQSMessage
        {
            MessageId = messageId,
            Body = body
        };
    }

    private static string CreateEnvelopeBody(
        string eventType,
        object payload,
        Guid? messageId = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        var payloadJson =
            JsonSerializer.Serialize(
                payload,
                payload.GetType(),
                JsonOptions);

        var envelope =
            new SqsIntegrationEventEnvelope(
                messageId ?? Guid.NewGuid(),
                eventType,
                occurredAtUtc ??
                    new DateTimeOffset(
                        2026,
                        9,
                        27,
                        10,
                        30,
                        0,
                        TimeSpan.Zero),
                payloadJson);

        return JsonSerializer.Serialize(
            envelope,
            JsonOptions);
    }

    private sealed class RecordingIntegrationEventDispatcher
        : IIntegrationEventDispatcher
    {
        public List<IIntegrationEvent> DispatchedEvents
        {
            get;
        } = [];

        public List<IntegrationEventDispatchContext> DispatchContexts
        {
            get;
        } = [];

        public Task DispatchAsync(
            IIntegrationEvent integrationEvent,
            IntegrationEventDispatchContext context,
            CancellationToken cancellationToken = default)
        {
            DispatchedEvents.Add(
                integrationEvent);

            DispatchContexts.Add(
                context);

            return Task.CompletedTask;
        }
    }

    private sealed class FailingIntegrationEventDispatcher
        : IIntegrationEventDispatcher
    {
        public int AttemptCount
        {
            get;
            private set;
        }

        public Task DispatchAsync(
            IIntegrationEvent integrationEvent,
            IntegrationEventDispatchContext context,
            CancellationToken cancellationToken = default)
        {
            AttemptCount++;

            throw new InvalidOperationException(
                "Synthetic downstream failure.");
        }
    }
    private sealed class RecordingIntegrationEventInbox
        : IIntegrationEventInbox
    {
        public IntegrationEventInboxClaimResult ClaimResult
        {
            get;
            set;
        } = IntegrationEventInboxClaimResult.Claimed;

        public List<Guid> ClaimedMessageIds
        {
            get;
        } = [];

        public List<Guid> ProcessedMessageIds
        {
            get;
        } = [];

        public List<Guid> ReleasedMessageIds
        {
            get;
        } = [];

        public Task<IntegrationEventInboxClaimResult> TryClaimAsync(
            Guid messageId,
            string workerId,
            DateTimeOffset utcNow,
            TimeSpan claimDuration,
            CancellationToken cancellationToken = default)
        {
            ClaimedMessageIds.Add(
                messageId);

            return Task.FromResult(
                ClaimResult);
        }

        public Task MarkProcessedAsync(
            Guid messageId,
            string workerId,
            DateTimeOffset processedAtUtc,
            CancellationToken cancellationToken = default)
        {
            ProcessedMessageIds.Add(
                messageId);

            return Task.CompletedTask;
        }

        public Task ReleaseAsync(
            Guid messageId,
            string workerId,
            CancellationToken cancellationToken = default)
        {
            ReleasedMessageIds.Add(
                messageId);

            return Task.CompletedTask;
        }
    }
    private sealed class TestLambdaContext
        : ILambdaContext
    {
        public string AwsRequestId =>
            "test-request";

        public IClientContext? ClientContext =>
            null;

        public string FunctionName =>
            "PGLN.Auth.AwsEmailWorker.Tests";

        public string FunctionVersion =>
            "test";

        public ICognitoIdentity? Identity =>
            null;

        public string InvokedFunctionArn =>
            "test";

        public ILambdaLogger Logger
        {
            get;
        } = new TestLambdaLogger();

        public string LogGroupName =>
            "test";

        public string LogStreamName =>
            "test";

        public int MemoryLimitInMB =>
            512;

        public TimeSpan RemainingTime =>
            TimeSpan.FromMinutes(1);
    }

    private sealed class TestLambdaLogger
        : ILambdaLogger
    {
        public void Log(
            string message)
        {
        }

        public void LogLine(
            string message)
        {
        }

        public void Log(
            string format,
            params object[] args)
        {
        }

        public void LogLine(
            string format,
            params object[] args)
        {
        }
    }
}

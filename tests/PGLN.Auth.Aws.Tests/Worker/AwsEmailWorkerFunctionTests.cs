using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
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

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var validEvent =
            CreateEmailConfirmationRequested();

        var validRecord =
            CreateRecord(
                "valid-message",
                CreateEnvelopeBody(
                    typeof(EmailConfirmationRequested).FullName!,
                    validEvent));

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

        var failure =
            Assert.Single(
                response.BatchItemFailures);

        Assert.Equal(
            "invalid-message",
            failure.ItemIdentifier);
    }

    [Fact]
    public async Task FunctionHandler_MalformedJson_ShouldReturnMessageAsFailed()
    {
        // Arrange
        var dispatcher =
            new RecordingIntegrationEventDispatcher();

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

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
    public async Task FunctionHandler_DispatcherFailure_ShouldReturnMessageAsFailed()
    {
        // Arrange
        var dispatcher =
            new FailingIntegrationEventDispatcher();

        var services =
            new ServiceCollection();

        services.AddSingleton<
            IntegrationEventTypeRegistry>();

        services.AddSingleton<
            IIntegrationEventDispatcher>(
                dispatcher);

        await using var provider =
            services.BuildServiceProvider();

        var function =
            new Function(provider);

        var integrationEvent =
            CreateEmailConfirmationRequested();

        var record =
            CreateRecord(
                "dispatcher-failure-message",
                CreateEnvelopeBody(
                    typeof(EmailConfirmationRequested).FullName!,
                    integrationEvent));

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
            1,
            dispatcher.AttemptCount);

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
        object payload)
    {
        var payloadJson =
            JsonSerializer.Serialize(
                payload,
                payload.GetType(),
                JsonOptions);

        var envelope =
            new SqsIntegrationEventEnvelope(
                Guid.NewGuid(),
                eventType,
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

        public Task DispatchAsync(
            IIntegrationEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            DispatchedEvents.Add(
                integrationEvent);

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
            CancellationToken cancellationToken = default)
        {
            AttemptCount++;

            throw new InvalidOperationException(
                "Synthetic downstream failure.");
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

using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Abstractions.Inbox;
using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.Aws.Events;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PGLN.Auth.Sample.AwsEmailWorker;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly TimeSpan ClaimDuration =
        TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IntegrationEventTypeRegistry _registry;

    public Function()
        : this(WorkerServices.Provider)
    {
    }

    public Function(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(
            serviceProvider);

        _scopeFactory =
            serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _registry =
            serviceProvider.GetRequiredService<IntegrationEventTypeRegistry>();
    }

    public async Task<SQSBatchResponse> FunctionHandler(
        SQSEvent sqsEvent,
        ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(
            sqsEvent);

        ArgumentNullException.ThrowIfNull(
            context);

        var failures =
            new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records ?? [])
        {
            try
            {
                await ProcessRecordAsync(
                    record,
                    context,
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                // Never log the SQS body or exception message.
                // Either may contain authentication or verification data.
                context.Logger.LogError(
                    $"SQS message {record.MessageId} failed: " +
                    $"{exception.GetType().Name}");

                failures.Add(
                    new SQSBatchResponse.BatchItemFailure
                    {
                        ItemIdentifier =
                            record.MessageId
                    });
            }
        }

        return new SQSBatchResponse(
            failures);
    }

    private async Task ProcessRecordAsync(
        SQSEvent.SQSMessage record,
        ILambdaContext context,
        CancellationToken cancellationToken)
    {
        var envelope =
            JsonSerializer.Deserialize<SqsIntegrationEventEnvelope>(
                record.Body,
                JsonOptions)
            ?? throw new JsonException(
                "SQS message does not contain a valid event envelope.");

        if (envelope.MessageId == Guid.Empty)
        {
            throw new JsonException(
                "SQS event envelope contains an empty message ID.");
        }

        var eventType =
            _registry.GetEventType(
                envelope.EventType);

        var integrationEvent =
            JsonSerializer.Deserialize(
                envelope.Payload,
                eventType,
                JsonOptions) as IIntegrationEvent
            ?? throw new JsonException(
                "SQS payload could not be deserialized as a known integration event.");

        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var inbox =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventInbox>();

        var dispatcher =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventDispatcher>();

        var workerId =
            CreateWorkerId(
                context,
                record);

        var claimedAtUtc =
            DateTimeOffset.UtcNow;

        var claimResult =
            await inbox.TryClaimAsync(
                envelope.MessageId,
                workerId,
                claimedAtUtc,
                ClaimDuration,
                cancellationToken);

        switch (claimResult)
        {
            case IntegrationEventInboxClaimResult.AlreadyProcessed:
                return;

            case IntegrationEventInboxClaimResult.AlreadyClaimed:
                throw new IntegrationEventAlreadyClaimedException(
                    envelope.MessageId);

            case IntegrationEventInboxClaimResult.Claimed:
                break;

            default:
                throw new InvalidOperationException(
                    $"Unknown inbox claim result: {claimResult}.");
        }

        try
        {
            var dispatchContext =
                new IntegrationEventDispatchContext(
                    MessageId:
                        envelope.MessageId,
                    OccurredAtUtc:
                        envelope.OccurredAtUtc);

            await dispatcher.DispatchAsync(
                integrationEvent,
                dispatchContext,
                cancellationToken);

            await inbox.MarkProcessedAsync(
                envelope.MessageId,
                workerId,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }
        catch
        {
            try
            {
                await inbox.ReleaseAsync(
                    envelope.MessageId,
                    workerId,
                    cancellationToken);
            }
            catch
            {
                // Preserve the original processing failure.
                // If release also fails, the lease will eventually expire.
            }

            throw;
        }
    }

    private static string CreateWorkerId(
        ILambdaContext context,
        SQSEvent.SQSMessage record)
    {
        var requestId =
            string.IsNullOrWhiteSpace(
                context.AwsRequestId)
                ? Guid.NewGuid().ToString("N")
                : context.AwsRequestId;

        var sqsMessageId =
            string.IsNullOrWhiteSpace(
                record.MessageId)
                ? Guid.NewGuid().ToString("N")
                : record.MessageId;

        return $"{requestId}:{sqsMessageId}";
    }

    private sealed class IntegrationEventAlreadyClaimedException
        : Exception
    {
        public IntegrationEventAlreadyClaimedException(
            Guid messageId)
            : base(
                $"Integration event {messageId} is already claimed.")
        {
        }
    }
}

using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Dispatching;
using PGLN.Auth.Aws.Events;
using Microsoft.Extensions.DependencyInjection;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PGLN.Auth.Sample.AwsEmailWorker;

public sealed class Function
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IntegrationEventTypeRegistry _registry;

    public Function()
        : this(WorkerServices.Provider)
    {
    }

    public Function(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _scopeFactory =
            serviceProvider.GetRequiredService<IServiceScopeFactory>();

        _registry =
            serviceProvider.GetRequiredService<IntegrationEventTypeRegistry>();
    }

    public async Task<SQSBatchResponse> FunctionHandler(
        SQSEvent sqsEvent,
        ILambdaContext context)
    {
        ArgumentNullException.ThrowIfNull(sqsEvent);
        ArgumentNullException.ThrowIfNull(context);

        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records ?? [])
        {
            try
            {
                await ProcessRecordAsync(
                    record,
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
                        ItemIdentifier = record.MessageId
                    });
            }
        }

        return new SQSBatchResponse(failures);
    }

    private async Task ProcessRecordAsync(
        SQSEvent.SQSMessage record,
        CancellationToken cancellationToken)
    {
        var envelope =
            JsonSerializer.Deserialize<SqsIntegrationEventEnvelope>(
                record.Body,
                JsonOptions)
            ?? throw new JsonException(
                "SQS message does not contain a valid event envelope.");

        // Only resolve event types explicitly registered by PGLN.Auth.
        var eventType =
            _registry.GetEventType(envelope.EventType);

        var integrationEvent =
            JsonSerializer.Deserialize(
                envelope.Payload,
                eventType,
                JsonOptions) as IIntegrationEvent
            ?? throw new JsonException(
                "SQS payload could not be deserialized as a known integration event.");

        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var dispatcher =
            scope.ServiceProvider
                .GetRequiredService<IIntegrationEventDispatcher>();

        await dispatcher.DispatchAsync(
            integrationEvent,
            cancellationToken);
    }
}

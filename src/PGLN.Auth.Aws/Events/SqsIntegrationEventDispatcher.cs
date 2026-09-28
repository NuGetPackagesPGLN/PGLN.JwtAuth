using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Application.Events.Dispatching;

namespace PGLN.Auth.Aws.Events;

public sealed class SqsIntegrationEventDispatcher
    : IIntegrationEventDispatcher
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IAmazonSQS _sqs;
    private readonly SqsOptions _options;

    public SqsIntegrationEventDispatcher(
        IAmazonSQS sqs,
        IOptions<SqsOptions> options)
    {
        ArgumentNullException.ThrowIfNull(sqs);
        ArgumentNullException.ThrowIfNull(options);

        _sqs = sqs;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.QueueUrl))
        {
            throw new InvalidOperationException(
                "AWS SQS queue URL is required.");
        }
    }

    public async Task DispatchAsync(
        IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            integrationEvent);

        var eventType =
            integrationEvent.GetType();

        var eventTypeName =
            IntegrationEventTypeRegistry
                .GetEventTypeName(eventType);

        var payload =
            JsonSerializer.Serialize(
                integrationEvent,
                eventType,
                SerializerOptions);

        var envelope =
            new SqsIntegrationEventEnvelope(
                MessageId: Guid.NewGuid(),
                EventType: eventTypeName,
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Payload: payload);

        var messageBody =
            JsonSerializer.Serialize(
                envelope,
                SerializerOptions);

        var request =
            new SendMessageRequest
            {
                QueueUrl = _options.QueueUrl,
                MessageBody = messageBody
            };

        await _sqs.SendMessageAsync(
            request,
            cancellationToken);
    }
}

namespace PGLN.Auth.Aws.Events;

/// <summary>
/// Transport envelope used when publishing a PGLN.Auth
/// integration event to Amazon SQS.
/// </summary>
public sealed record SqsIntegrationEventEnvelope(
    Guid MessageId,
    string EventType,
    DateTimeOffset OccurredAtUtc,
    string Payload);

namespace PGLN.Auth.Application.Abstractions.Events;

public sealed record IntegrationEventDispatchContext(
    Guid MessageId,
    DateTimeOffset OccurredAtUtc);

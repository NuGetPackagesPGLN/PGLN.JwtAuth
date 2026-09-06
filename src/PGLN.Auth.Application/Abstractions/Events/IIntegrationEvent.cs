namespace PGLN.Auth.Application.Abstractions.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}

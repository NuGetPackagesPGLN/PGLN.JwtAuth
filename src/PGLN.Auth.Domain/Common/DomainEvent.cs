namespace PGLN.Auth.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
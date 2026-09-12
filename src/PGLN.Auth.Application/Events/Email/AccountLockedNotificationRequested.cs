using PGLN.Auth.Application.Abstractions.Events;
using PGLN.Auth.Domain.Users;

namespace PGLN.Auth.Application.Events.Email;

public sealed record AccountLockedNotificationRequested(
    Guid EventId,
    UserId UserId,
    string Email,
    DateTimeOffset LockedUntilUtc,
    DateTimeOffset OccurredAtUtc)
    : IIntegrationEvent;
